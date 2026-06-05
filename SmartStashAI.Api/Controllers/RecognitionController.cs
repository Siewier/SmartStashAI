using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using SmartStashAI.Shared.Dtos;
using System.Text.Json;

namespace SmartStashAI.Api.Controllers;

public class ImageRecognitionRequest
{
    public string Base64Image { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class RecognitionController : ControllerBase
{
    private readonly IChatClient _chatClient;

    public RecognitionController(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    [HttpPost("recognize")]
    public async Task<ActionResult<RecognizedItemDto>> RecognizeItem([FromBody] ImageRecognitionRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Base64Image))
                return BadRequest("Brak danych obrazu.");

            // FIX: Usunięto rolę "system". Całość instrukcji przeniesiono do roli "user".
            var instruction = "Jesteś ekspertem od katalogowania przedmiotów. Zidentyfikuj przedmiot na tym zdjęciu. Zwróć WYŁĄCZNIE czysty JSON w formacie: {\"name\": \"nazwa\", \"category\": \"kategoria\", \"purpose\": \"przeznaczenie\"}. Nie dodawaj żadnego tekstu przed ani po JSONie.";

            var messages = new List<object>();

            messages.Add(new
            {
                role = "user",
                content = new List<object>
            {
                new { type = "text", text = instruction },
                new { type = "image_url", image_url = new { url = request.Base64Image } }
            }
            });

            var payload = new
            {
                model = "llava-1.6-mistral-7b",
                messages = messages,
                temperature = 0.1
            };

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(90); // Wydłużony czas dla pewności

            var response = await client.PostAsJsonAsync("http://192.168.68.65:1234/v1/chat/completions", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Błąd połączenia z LM Studio: {errorDetails}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Wyciągnięcie odpowiedzi
            string jsonResponse = result.GetProperty("choices")[0]
                                        .GetProperty("message")
                                        .GetProperty("content")
                                        .GetString() ?? "";

            // Oczyszczanie odpowiedzi (LLM często dodają Markdown)
            jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

            var recognizedItem = JsonSerializer.Deserialize<RecognizedItemDto>(
                jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return Ok(recognizedItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd przetwarzania: {ex.Message}");
        }
    }
}