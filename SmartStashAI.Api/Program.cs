using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using OllamaSharp;
using SmartStashAI.Api.Data;
using SmartStashAI.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Rejestracja bazy i us³ug AI/Auth
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=smartstash.db"));

builder.Services.AddSingleton<IChatClient>(sp =>
    new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2-vision:11b"));

builder.Services.AddScoped<IAuthService, AuthService>();

// 2. Konfiguracja zabezpieczeñ JWT
var secretKey = builder.Configuration["Jwt:Secret"] ?? "SuperTajnyKluczDoSmartStashAI2026!TrzymajGoWBezpiecznymMiejscu";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SmartStashAI.Api", Version = "v1" });

    // Dodanie konfiguracji dla k³ódeczki i tokenu JWT w Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Wklej token JWT w formacie: Bearer {twój_token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// 3. WA¯NE: W³¹czenie middleware autentykacji (musi byæ PRZED UseAuthorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();