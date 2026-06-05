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
            {
                return BadRequest("Przesłane żądanie nie zawiera danych obrazu.");
            }

            string base64Data = request.Base64Image;

            if (base64Data.Contains(","))
            {
                base64Data = base64Data.Split(',')[1];
            }

            var imageBytes = Convert.FromBase64String(base64Data);
            var imageContent = new DataContent(imageBytes, "image/jpeg");

            var systemPrompt = @"Jesteś ekspertem od katalogowania elektroniki, komponentów automatyki, narzędzi i przedmiotów domowych.
Przeanalizuj przesłane zdjęcie i zwróć obiekt JSON reprezentujący zidentyfikowany przedmiot.
Wszystkie wartości pól muszą być w języku polskim. Wybierz precyzyjną, logiczną kategorię (np. Okablowanie, Mikrokontrolery, Elektronika, Narzędzia ręczne).

Oczekiwany format wyjściowy to ścisły, surowy JSON bez żadnego dodatkowego komentarza:
{
  ""name"": ""Dokładna nazwa przedmiotu"",
  ""category"": ""Kategoria przedmiotu"",
  ""purpose"": ""Krótkie przeznaczenie przedmiotu""
}";

            var userPrompt = "Zidentyfikuj przedmiot na tym zdjęciu i uzupełnij strukturę JSON.";

            var options = new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json,
                Temperature = 0.1f
            };

            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, userPrompt) { Contents = { imageContent } }
            };

            // 1. Wywołanie metody natywnej: GetResponseAsync
            ChatResponse response = await _chatClient.GetResponseAsync(messages, options);

            // 2. Pobranie tekstu za pomocą właściwości .Text, którą potwierdziliśmy w kodzie źródłowym
            string jsonResponse = response.Text;

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                return StatusCode(500, "Model AI zwrócił pustą odpowiedź.");
            }

            if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
            }

            var recognizedItem = JsonSerializer.Deserialize<RecognizedItemDto>(
                jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (recognizedItem == null)
            {
                return StatusCode(500, "Nie udało się poprawnie sparsować odpowiedzi strukturalnej z AI.");
            }

            return Ok(recognizedItem);
        }
        catch (FormatException)
        {
            return BadRequest("Przesłany ciąg tekstowy nie jest poprawnym formatem Base64.");
        }
        catch (Exception ex)
        {
            return StatusCode(503, $"Usługa AI (Ollama) jest obecnie niedostępna lub wystąpił błąd komunikacji. Szczegóły: {ex.Message}");
        }
    }
}