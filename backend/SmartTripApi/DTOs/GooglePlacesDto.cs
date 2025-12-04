using System.Text.Json.Serialization;

namespace SmartTripApi.DTOs
{
    
    public class GooglePlacesTextSearchResponse
    {
        [JsonPropertyName("results")]
        public List<GooglePlaceResult> Results { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class GooglePlaceResult
    {
        [JsonPropertyName("place_id")]
        public string PlaceId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; } = string.Empty;

        [JsonPropertyName("geometry")]
        public GoogleGeometry? Geometry { get; set; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("user_ratings_total")]
        public int? UserRatingsTotal { get; set; }

        [JsonPropertyName("price_level")]
        public int? PriceLevel { get; set; }

        [JsonPropertyName("opening_hours")]
        public GoogleOpeningHours? OpeningHours { get; set; }

        [JsonPropertyName("photos")]
        public List<GooglePhoto>? Photos { get; set; }

        [JsonPropertyName("reviews")]
        public List<GoogleReview>? Reviews { get; set; }
    }

    public class GoogleGeometry
    {
        [JsonPropertyName("location")]
        public GoogleLocation? Location { get; set; }
    }

    public class GoogleLocation
    {
        [JsonPropertyName("lat")]
        public decimal Lat { get; set; }

        [JsonPropertyName("lng")]
        public decimal Lng { get; set; }
    }

    public class GoogleOpeningHours
    {
        [JsonPropertyName("open_now")]
        public bool? OpenNow { get; set; }

        [JsonPropertyName("weekday_text")]
        public List<string>? WeekdayText { get; set; }
    }

    public class GooglePhoto
    {
        [JsonPropertyName("photo_reference")]
        public string PhotoReference { get; set; } = string.Empty;

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    public class GoogleReview
    {
        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("profile_photo_url")]
        public string? ProfilePhotoUrl { get; set; }

        [JsonPropertyName("time")]
        public long? Time { get; set; }  // Unix timestamp

        [JsonPropertyName("relative_time_description")]
        public string? RelativeTimeDescription { get; set; }
    }

    public class GooglePlaceDetailsResponse
    {
        [JsonPropertyName("result")]
        public GooglePlaceDetailsResult? Result { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class GooglePlaceDetailsResult
    {
        [JsonPropertyName("reviews")]
        public List<GoogleReview>? Reviews { get; set; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("user_ratings_total")]
        public int? UserRatingsTotal { get; set; }

        [JsonPropertyName("address_components")]
        public List<GoogleAddressComponent>? AddressComponents { get; set; }

        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }
    }

    public class GoogleAddressComponent
    {
        [JsonPropertyName("long_name")]
        public string LongName { get; set; } = string.Empty;

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = new();
    }

    public class LocationInfoDto
    {
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? FormattedAddress { get; set; }
    }
}
