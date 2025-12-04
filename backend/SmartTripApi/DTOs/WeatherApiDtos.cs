using System.Text.Json.Serialization;

namespace SmartTripApi.DTOs
{
    public class WeatherApiForecastResponse
    {
        [JsonPropertyName("forecast")]
        public WeatherApiForecast Forecast { get; set; } = null!;
    }

    public class WeatherApiForecast
    {
        [JsonPropertyName("forecastday")]
        public List<WeatherApiForecastDay> Forecastday { get; set; } = new();
    }

    public class WeatherApiForecastDay
    {
        [JsonPropertyName("day")]
        public WeatherApiDay Day { get; set; } = null!;
    }

    public class WeatherApiDay
    {
        [JsonPropertyName("maxtemp_c")]
        public double MaxtempC { get; set; }

        [JsonPropertyName("mintemp_c")]
        public double MintempC { get; set; }

        [JsonPropertyName("avgtemp_c")]
        public double AvgtempC { get; set; }

        [JsonPropertyName("maxwind_kph")]
        public double MaxwindKph { get; set; }

        [JsonPropertyName("avghumidity")]
        public double Avghumidity { get; set; }

        [JsonPropertyName("condition")]
        public WeatherApiCondition Condition { get; set; } = null!;
    }

    public class WeatherApiCondition
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}
