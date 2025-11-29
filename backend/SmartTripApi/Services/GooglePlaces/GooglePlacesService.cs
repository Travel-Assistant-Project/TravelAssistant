using System.Text.Json;
using System.Web;
using SmartTripApi.DTOs;

namespace SmartTripApi.Services.GooglePlaces
{
    public class GooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GooglePlacesService> _logger;
        private readonly string? _apiKey;

        public GooglePlacesService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<GooglePlacesService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["GooglePlaces:ApiKey"];
        }

        public async Task<GooglePlaceResult?> SearchPlaceAsync(string placeName, string? region = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Google Places API key is not configured");
                    return null;
                }

                // Build query with region if provided
                var query = string.IsNullOrEmpty(region) 
                    ? placeName 
                    : $"{placeName}, {region}";

                var encodedQuery = HttpUtility.UrlEncode(query);
                var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={encodedQuery}&key={_apiKey}";

                _logger.LogInformation("Searching Google Places for: {Query}", query);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GooglePlacesTextSearchResponse>(content);

                if (result?.Status != "OK" || result.Results.Count == 0)
                {
                    _logger.LogWarning("No results found for place: {PlaceName}. Status: {Status}", 
                        placeName, result?.Status);
                    return null;
                }

                // Return the first (most relevant) result
                return result.Results.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for place: {PlaceName}", placeName);
                return null;
            }
        }

        /// <summary>
        /// Get photo URL from photo reference
        /// </summary>
        public string GetPhotoUrl(string photoReference, int maxWidth = 400)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return string.Empty;
            }

            return $"https://maps.googleapis.com/maps/api/place/photo?maxwidth={maxWidth}&photo_reference={photoReference}&key={_apiKey}";
        }

        /// <summary>
        /// Get multiple photo URLs from a place result
        /// </summary>
        public List<string> GetPhotoUrls(GooglePlaceResult place, int count = 3, int maxWidth = 400)
        {
            var photoUrls = new List<string>();

            if (place.Photos == null || !place.Photos.Any())
            {
                return photoUrls;
            }

            var photosToTake = Math.Min(count, place.Photos.Count);
            
            for (int i = 0; i < photosToTake; i++)
            {
                var photoUrl = GetPhotoUrl(place.Photos[i].PhotoReference, maxWidth);
                if (!string.IsNullOrEmpty(photoUrl))
                {
                    photoUrls.Add(photoUrl);
                }
            }

            return photoUrls;
        }

        /// <summary>
        /// Get Google Maps URL for a place
        /// </summary>
        public string GetGoogleMapsUrl(string placeId)
        {
            return $"https://www.google.com/maps/place/?q=place_id:{placeId}";
        }

        /// <summary>
        /// Get detailed place information including reviews using Place Details API
        /// </summary>
        public async Task<GooglePlaceDetailsResult?> GetPlaceDetailsAsync(string placeId)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Google Places API key is not configured");
                    return null;
                }

                // Request reviews field specifically
                var fields = "reviews,rating,user_ratings_total";
                var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields={fields}&key={_apiKey}";

                _logger.LogInformation("Fetching place details for place_id: {PlaceId}", placeId);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GooglePlaceDetailsResponse>(content);

                if (result?.Status != "OK")
                {
                    _logger.LogWarning("Failed to get place details for {PlaceId}. Status: {Status}", 
                        placeId, result?.Status);
                    return null;
                }

                return result.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting place details for: {PlaceId}", placeId);
                return null;
            }
        }

        /// <summary>
        /// Get place details with address components for location extraction
        /// </summary>
        public async Task<GooglePlaceDetailsResult?> GetPlaceDetailsWithAddressAsync(string placeId)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Google Places API key is not configured");
                    return null;
                }

                // Request address_components and formatted_address
                var fields = "address_components,formatted_address";
                var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields={fields}&key={_apiKey}";

                _logger.LogInformation("Fetching place details with address for place_id: {PlaceId}", placeId);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GooglePlaceDetailsResponse>(content);

                if (result?.Status != "OK")
                {
                    _logger.LogWarning("Failed to get place details for {PlaceId}. Status: {Status}", 
                        placeId, result?.Status);
                    return null;
                }

                return result.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting place details with address for: {PlaceId}", placeId);
                return null;
            }
        }

        /// <summary>
        /// Extract country and city from address components
        /// </summary>
        public LocationInfoDto? ExtractLocationInfo(GooglePlaceDetailsResult? placeDetails)
        {
            if (placeDetails?.AddressComponents == null || !placeDetails.AddressComponents.Any())
            {
                return null;
            }

            string? country = null;
            string? city = null;

            foreach (var component in placeDetails.AddressComponents)
            {
                // Country extraction
                if (component.Types.Contains("country"))
                {
                    country = component.LongName;
                }

                // City extraction - try different types
                if (component.Types.Contains("locality"))
                {
                    city = component.LongName;
                }
                else if (city == null && component.Types.Contains("administrative_area_level_1"))
                {
                    // Fallback to state/province if no city found
                    city = component.LongName;
                }
                else if (city == null && component.Types.Contains("administrative_area_level_2"))
                {
                    // Fallback to county if no city found
                    city = component.LongName;
                }
            }

            return new LocationInfoDto
            {
                Country = country,
                City = city,
                FormattedAddress = placeDetails.FormattedAddress
            };
        }
    }
}
