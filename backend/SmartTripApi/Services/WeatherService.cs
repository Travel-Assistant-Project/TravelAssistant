using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTripApi.Data;
using SmartTripApi.DTOs;
using SmartTripApi.Models;

namespace SmartTripApi.Services.Weather
{
    public interface IWeatherService
    {
        Task UpdateItineraryWeatherAsync(int itineraryId, CancellationToken cancellationToken = default);
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _lang;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public WeatherService(
            HttpClient httpClient,
            IConfiguration configuration,
            AppDbContext context,
            ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;

            // WeatherAPI.com → WeatherApi:ApiKey veya ortam değişkeni WEATHERAPI_KEY
            _apiKey =
                configuration["WeatherApi:ApiKey"]
                ?? Environment.GetEnvironmentVariable("WEATHERAPI_KEY")
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException(
                    "WeatherAPI API key not found. Please set WEATHERAPI_KEY in .env file or config.");

            _baseUrl = configuration["WeatherApi:BaseUrl"] ?? "https://api.weatherapi.com/v1";
            _lang = configuration["WeatherApi:Lang"] ?? "tr";
        }

        private async Task<WeatherApiForecastDay?> GetDailyForecastAsync(
            double lat,
            double lon,
            CancellationToken cancellationToken = default)
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);

            // WeatherAPI forecast endpoint:
            // /forecast.json?key=API_KEY&q=LAT,LON&days=1&aqi=no&alerts=no&lang=tr
            var url =
                $"{_baseUrl}/forecast.json?key={_apiKey}" +
                $"&q={latStr},{lonStr}" +
                $"&days=1&aqi=no&alerts=no&lang={_lang}";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogWarning(
                        "WeatherAPI error: {StatusCode}. Body: {Body}",
                        response.StatusCode,
                        body
                    );

                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var data = await JsonSerializer.DeserializeAsync<WeatherApiForecastResponse>(
                    stream, JsonOptions, cancellationToken);

                return data?.Forecast?.Forecastday?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling WeatherAPI");
                return null;
            }
        }

        public async Task UpdateItineraryWeatherAsync(
            int itineraryId,
            CancellationToken cancellationToken = default)
        {
            var itinerary = await _context.Itineraries
                .Include(i => i.ItineraryDays)
                    .ThenInclude(d => d.Activities)
                        .ThenInclude(a => a.Place)
                .FirstOrDefaultAsync(i => i.Id == itineraryId, cancellationToken);

            if (itinerary == null)
            {
                _logger.LogWarning("Itinerary {ItineraryId} not found", itineraryId);
                return;
            }

            foreach (var day in itinerary.ItineraryDays)
            {
                var firstActivity = day.Activities
                    .OrderBy(a => a.StartTime)
                    .FirstOrDefault();

                if (firstActivity?.Place == null ||
                    !firstActivity.Place.Latitude.HasValue ||
                    !firstActivity.Place.Longitude.HasValue)
                {
                    _logger.LogInformation("ItineraryDay {DayId} has no valid place coordinates", day.Id);
                    continue;
                }

                var lat = (double)firstActivity.Place.Latitude.Value;
                var lon = (double)firstActivity.Place.Longitude.Value;

                var forecastDay = await GetDailyForecastAsync(lat, lon, cancellationToken);
                if (forecastDay == null)
                    continue;

                var dayInfo = forecastDay.Day;
                var condition = dayInfo.Condition;

                var summaryObject = new
                {
                    temp = new
                    {
                        day = dayInfo.AvgtempC,
                        min = dayInfo.MintempC,
                        max = dayInfo.MaxtempC
                    },
                    weather = new
                    {
                        main = condition.Text,
                        description = condition.Text
                    },
                    humidity = dayInfo.Avghumidity,
                    windSpeed = dayInfo.MaxwindKph,
                    source = "weatherapi_forecast"
                };

                day.WeatherInfo = JsonSerializer.SerializeToDocument(summaryObject, JsonOptions);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
