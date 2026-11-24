using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartTripApi.Data;
using SmartTripApi.Services;
using SmartTripApi.Models;
using SmartTripApi.Services.AI;
using SmartTripApi.Services.GooglePlaces;
using SmartTripApi.Services.Weather;
using SmartTripApi.Services.RoutePlanning;
using SmartTripApi.Services.Events;
using System.Text;
using DotNetEnv;
using StackExchange.Redis;
using SmartTripApi.Services.RoutePlanning.Caching;


// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

static string GetRequiredEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Required environment variable '{name}' is not configured. " +
            "Copy .env.example to .env and provide a non-placeholder value.");
    }

    return value;
}

var configuredOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(configuredOrigins))
{
    throw new InvalidOperationException(
        "CORS_ALLOWED_ORIGINS must be configured in production.");
}

var allowedOrigins = (configuredOrigins ?? "http://localhost:8081,http://localhost:19006")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Redis (Cache)
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(redisConn);
});

// Cache services
builder.Services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
builder.Services.AddSingleton<IRouteCacheService, RedisRouteCacheService>();

builder.Services.AddHttpContextAccessor();

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "travelassistant";
var dbUser = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "postgres";
var dbPass = GetRequiredEnvironmentVariable("DB_PASSWORD");

// Build connection string from environment variables
// Set timezone to Turkey (UTC+3)
var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass};Timezone=Europe/Istanbul";

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

var secret = GetRequiredEnvironmentVariable("JWT_SECRET");
if (Encoding.UTF8.GetByteCount(secret) < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET must contain at least 32 bytes of unpredictable data.");
}

builder.Configuration["Jwt:Secret"] = secret;
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

// HttpClient + servis kayıtları
builder.Services.AddHttpClient();

// AI
builder.Services.AddScoped<PromptBuilder>();
builder.Services.AddScoped<AIService>();

// Google Places
builder.Services.AddScoped<GooglePlacesService>();
builder.Services.AddScoped<PlaceEnrichmentService>();

// Transport
builder.Services.AddScoped<TransportEnrichmentService>();

// WeatherService (Typed HttpClient)
builder.Services.AddHttpClient<IWeatherService, WeatherService>();

//Route Planning Service 
builder.Services.AddScoped<IRoutePlanService, RoutePlanService>();

// Events Services
builder.Services.AddHttpClient<TicketmasterService>();
builder.Services.AddHttpClient<EventbriteService>();
builder.Services.AddScoped<IEventService, HybridEventService>();

// User Analysis Service
builder.Services.AddScoped<UserAnalysisService>();

// Read API keys from .env file and override appsettings
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
var weatherApiKey = Environment.GetEnvironmentVariable("WEATHERAPI_KEY");
var ticketmasterApiKey = Environment.GetEnvironmentVariable("TICKETMASTER_API_KEY");
var eventbriteApiToken = Environment.GetEnvironmentVariable("EVENTBRITE_API_TOKEN");

if (!string.IsNullOrEmpty(geminiApiKey))
{
    builder.Configuration["AIProviders:Gemini:ApiKey"] = geminiApiKey;
}

if (!string.IsNullOrEmpty(googleApiKey))
{
    builder.Configuration["GooglePlaces:ApiKey"] = googleApiKey;
}

if (!string.IsNullOrEmpty(weatherApiKey))
{
    // Yeni config path → WeatherApi:ApiKey
    builder.Configuration["WeatherApi:ApiKey"] = weatherApiKey;
}

if (!string.IsNullOrEmpty(ticketmasterApiKey))
{
    builder.Configuration["TicketmasterApi:ApiKey"] = ticketmasterApiKey;
}

if (!string.IsNullOrEmpty(eventbriteApiToken))
{
    builder.Configuration["EventbriteApi:Token"] = eventbriteApiToken;
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

app.UseCors("ConfiguredOrigins");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
