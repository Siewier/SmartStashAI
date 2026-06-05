using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SmartStashAI.Client;
using SmartStashAI.Client.Auth;

// 1. Inicjalizacja buildera z poprawnym przekazaniem tablicy args
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 2. Konfiguracja HttpClient – celujemy w Twój lokalny backend API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7047/") });

// 3. Rejestracja us³ug zarz¹dzania stanem autoryzacji i tokenów JWT
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

await builder.Build().RunAsync();