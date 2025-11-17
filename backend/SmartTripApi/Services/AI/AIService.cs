using System.Text;
using System.Text.Json;
using SmartTripApi.DTOs;

namespace SmartTripApi.Services.AI
{
    public class AIService
    {
        public readonly HttpClient _httpClient;
        private readonly PromptBuilder _promptBuilder;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public AIService(HttpClient httpClient, PromptBuilder promptBuilder, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _promptBuilder = promptBuilder;
            _configuration = configuration;
            
            // Direkt environment variable'dan oku (.env dosyası Program.cs'de yüklenmiş)
            _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key not found. Please set GEMINI_API_KEY in .env file.");
            }
        }

        public async Task<TripPlanResponse> GenerateTripPlanAsync(TripPlanRequest request)
        {
            try
            {
                var prompt = _promptBuilder.BuildTripPlanPrompt(request);
                
                var geminiRequest = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(geminiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}", 
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Gemini API response'undan text'i çıkar
                var generatedText = geminiResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new Exception("Empty response from Gemini API");
                }

                // JSON'ı temizle ve parse et
                var cleanJson = ExtractJsonFromResponse(generatedText);
                var tripPlan = JsonSerializer.Deserialize<TripPlanResponse>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return tripPlan ?? throw new Exception("Failed to parse AI response");
            }
            catch (Exception ex)
            {
                throw new Exception($"AI service error: {ex.Message}", ex);
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            // JSON'ı bul ve temizle
            var startIndex = response.IndexOf('{');
            var lastIndex = response.LastIndexOf('}');

            if (startIndex == -1 || lastIndex == -1 || startIndex >= lastIndex)
            {
                throw new Exception("No valid JSON found in AI response");
            }

            return response.Substring(startIndex, lastIndex - startIndex + 1);
        }
    }
}
