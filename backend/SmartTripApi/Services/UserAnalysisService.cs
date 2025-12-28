using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.Models;
using System.Text.Json;
using System.Text;

namespace SmartTripApi.Services
{
    public class UserAnalysisService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserAnalysisService> _logger;

        public UserAnalysisService(
            AppDbContext context,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<UserAnalysisService> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<UserAnalysisResult> AnalyzeUserPreferencesAsync(int userId)
        {
            // Kullanıcının favori rotalarını getir
            var favoriteItineraries = await _context.Favorites
                .Where(f => f.UserId == userId && f.ItineraryId != null)
                .Include(f => f.Itinerary)
                .Select(f => f.Itinerary!)
                .ToListAsync();

            // Kullanıcının favori mekanlarını getir
            var favoritePlaces = await _context.Favorites
                .Where(f => f.UserId == userId && f.PlaceId != null)
                .Include(f => f.Place)
                .Select(f => f.Place!)
                .ToListAsync();

            // Kullanıcının oluşturduğu rotaları getir
            var userItineraries = await _context.Itineraries
                .Where(i => i.UserId == userId)
                .ToListAsync();

            // Analiz için veri hazırla
            var analysisData = PrepareAnalysisData(favoriteItineraries, favoritePlaces, userItineraries);

            // AI'dan öneriler al
            var recommendations = await GetAIRecommendationsAsync(analysisData);

            return new UserAnalysisResult
            {
                TotalFavoriteItineraries = favoriteItineraries.Count,
                TotalFavoritePlaces = favoritePlaces.Count,
                TotalCreatedItineraries = userItineraries.Count,
                PreferredThemes = GetPreferredThemes(favoriteItineraries, favoritePlaces),
                PreferredRegions = GetPreferredRegions(favoriteItineraries, favoritePlaces),
                PreferredBudgetRange = GetPreferredBudgetRange(favoriteItineraries),
                PreferredIntensity = GetPreferredIntensity(favoriteItineraries),
                AIRecommendations = recommendations
            };
        }

        private string PrepareAnalysisData(
            List<Itinerary> favoriteItineraries,
            List<Place> favoritePlaces,
            List<Itinerary> userItineraries)
        {
            var data = new StringBuilder();
            
            data.AppendLine("User Travel Preferences Analysis:");
            data.AppendLine();
            
            // Favori rotalar analizi
            if (favoriteItineraries.Any())
            {
                data.AppendLine($"Favorite Itineraries ({favoriteItineraries.Count}):");
                foreach (var itinerary in favoriteItineraries.Take(5))
                {
                    data.AppendLine($"- {itinerary.Name} in {itinerary.Region}");
                    data.AppendLine($"  Theme: {itinerary.Theme}, Budget: {itinerary.Budget}, Days: {itinerary.DaysCount}");
                }
                data.AppendLine();
            }

            // Favori mekanlar analizi
            if (favoritePlaces.Any())
            {
                data.AppendLine($"Favorite Places ({favoritePlaces.Count}):");
                foreach (var place in favoritePlaces.Take(5))
                {
                    data.AppendLine($"- {place.Name}");
                    data.AppendLine($"  Category: {place.Category}, Location: {place.City}, {place.Country}");
                }
                data.AppendLine();
            }

            // Kullanıcının oluşturduğu rotalar
            if (userItineraries.Any())
            {
                data.AppendLine($"Created Itineraries ({userItineraries.Count}):");
                var themes = userItineraries.GroupBy(i => i.Theme).OrderByDescending(g => g.Count());
                foreach (var theme in themes.Take(3))
                {
                    data.AppendLine($"- Theme {theme.Key}: {theme.Count()} trips");
                }
            }

            return data.ToString();
        }

        private async Task<string> GetAIRecommendationsAsync(string analysisData)
        {
            try
            {
                // OpenAI yerine Gemini kullan
                var apiKey = _configuration["GEMINI_API_KEY"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return "AI recommendations are not available at this time.";
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $@"You are a travel recommendation expert. Based on the user's favorite trips and places, provide personalized travel suggestions. Keep recommendations concise, friendly, and specific. Focus on destinations, themes, and activities the user would enjoy.

{analysisData}

Based on this user's travel preferences, provide 3 personalized travel recommendations. Each recommendation should include a destination and why they would love it. Keep it short and engaging. Write in English. Format the response in a clean, readable way with minimal special characters - use simple formatting like numbers for listing items."
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    return result
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Öneriler şu anda hazırlanamadı.";
                }

                _logger.LogWarning($"Gemini API returned status code: {response.StatusCode}");
                return "Öneriler oluşturulamadı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI recommendations");
                return "AI önerileri geçici olarak kullanılamıyor.";
            }
        }

        private List<string> GetPreferredThemes(List<Itinerary> itineraries, List<Place> places)
        {
            var themes = new List<ThemeTypeEnum>();
            
            // Itinerary.Theme nullable olduğu için sadece HasValue olanları ekle
            var itineraryThemes = itineraries
                .Where(i => i.Theme.HasValue)
                .Select(i => i.Theme!.Value)
                .ToList();
            
            themes.AddRange(itineraryThemes);
            
            // Place.Category nullable olduğu için sadece HasValue olanları ekle
            var placeThemes = places
                .Where(p => p.Category.HasValue)
                .Select(p => p.Category!.Value)
                .ToList();
            
            themes.AddRange(placeThemes);

            return themes
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key.ToString())
                .ToList();
        }

        private List<string> GetPreferredRegions(List<Itinerary> itineraries, List<Place> places)
        {
            var regions = new List<string>();
            
            regions.AddRange(itineraries.Select(i => i.Region));
            regions.AddRange(places.Where(p => !string.IsNullOrEmpty(p.City)).Select(p => p.City!));

            return regions
                .Where(r => !string.IsNullOrEmpty(r))
                .GroupBy(r => r)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();
        }

        private string GetPreferredBudgetRange(List<Itinerary> itineraries)
        {
            if (!itineraries.Any())
                return "Not enough data";

            var avgBudget = itineraries
                .Where(i => i.Budget != null)
                .Average(i => (int)i.Budget!);
            
            if (avgBudget <= 1.5) return "Budget-friendly";
            if (avgBudget <= 2.5) return "Moderate";
            return "Luxury";
        }

        private string GetPreferredIntensity(List<Itinerary> itineraries)
        {
            if (!itineraries.Any())
                return "Not enough data";

            var avgIntensity = itineraries
                .Where(i => i.Intensity != null)
                .Average(i => (int)i.Intensity!);
            
            if (avgIntensity <= 1.5) return "Relaxed";
            if (avgIntensity <= 2.5) return "Moderate";
            return "Intense";
        }
    }

    public class UserAnalysisResult
    {
        public int TotalFavoriteItineraries { get; set; }
        public int TotalFavoritePlaces { get; set; }
        public int TotalCreatedItineraries { get; set; }
        public List<string> PreferredThemes { get; set; } = new();
        public List<string> PreferredRegions { get; set; } = new();
        public string PreferredBudgetRange { get; set; } = string.Empty;
        public string PreferredIntensity { get; set; } = string.Empty;
        public string AIRecommendations { get; set; } = string.Empty;
    }
}
