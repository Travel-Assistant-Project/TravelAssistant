using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartTripApi.Data;
using SmartTripApi.Services;
using SmartTripApi.Models;
using System.Text;
using DotNetEnv;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "travelassistant";
var dbUser = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "postgres";
var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

// Build connection string from environment variables
var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass}";

// Map PostgreSQL enums
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => 
    {
        o.MapEnum<BudgetLevelEnum>("budget_level");
        o.MapEnum<IntensityLevelEnum>("intensity_level");
        o.MapEnum<TransportModeEnum>("transport_mode");
        o.MapEnum<ThemeTypeEnum>("theme_type");
        o.MapEnum<UserRoleEnum>("user_role");
    });
});

builder.Services.AddScoped<JwtTokenService>();

var secret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddHttpClient();
builder.Services.AddScoped<SmartTripApi.Services.AI.PromptBuilder>();
builder.Services.AddScoped<SmartTripApi.Services.AI.AIService>();

builder.Services.AddScoped<SmartTripApi.Services.GooglePlaces.GooglePlacesService>();
builder.Services.AddScoped<SmartTripApi.Services.GooglePlaces.PlaceEnrichmentService>();

// Read API keys from .env file and override appsettings
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

if (!string.IsNullOrEmpty(geminiApiKey))
{
    builder.Configuration["AIProviders:Gemini:ApiKey"] = geminiApiKey;
}

if (!string.IsNullOrEmpty(googleApiKey))
{
    builder.Configuration["GooglePlaces:ApiKey"] = googleApiKey;
}


var app = builder.Build();

// Swagger middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartTrip API v1");
        c.RoutePrefix = string.Empty; // Swagger served at root
    });
}

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
