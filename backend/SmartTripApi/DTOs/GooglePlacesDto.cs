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

    // --- Directions API Models ---

    public class GoogleDirectionsResponse
    {
        [JsonPropertyName("routes")]
        public List<GoogleDirectionRoute> Routes { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class GoogleDirectionRoute
    {
        [JsonPropertyName("legs")]
        public List<GoogleDirectionLeg> Legs { get; set; } = new();
        
        [JsonPropertyName("overview_polyline")]
        public GooglePolyline? OverviewPolyline { get; set; }
    }

    public class GoogleDirectionLeg
    {
        [JsonPropertyName("distance")]
        public GoogleTextValue? Distance { get; set; }

        [JsonPropertyName("duration")]
        public GoogleTextValue? Duration { get; set; }

        [JsonPropertyName("steps")]
        public List<GoogleDirectionStep> Steps { get; set; } = new();
    }

    public class GoogleDirectionStep
    {
        [JsonPropertyName("distance")]
        public GoogleTextValue? Distance { get; set; }

        [JsonPropertyName("duration")]
        public GoogleTextValue? Duration { get; set; }

        [JsonPropertyName("html_instructions")]
        public string HtmlInstructions { get; set; } = string.Empty;

        [JsonPropertyName("travel_mode")]
        public string TravelMode { get; set; } = string.Empty;

        [JsonPropertyName("transit_details")]
        public GoogleTransitDetails? TransitDetails { get; set; }
        
        [JsonPropertyName("polyline")]
        public GooglePolyline? Polyline { get; set; }
    }

    public class GoogleTransitDetails
    {
        [JsonPropertyName("line")]
        public GoogleTransitLine? Line { get; set; }

        [JsonPropertyName("num_stops")]
        public int NumStops { get; set; }
        
        [JsonPropertyName("departure_stop")]
        public GoogleTransitStop? DepartureStop { get; set; }
        
        [JsonPropertyName("arrival_stop")]
        public GoogleTransitStop? ArrivalStop { get; set; }
    }

    public class GoogleTransitLine
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("vehicle")]
        public GoogleTransitVehicle? Vehicle { get; set; }
    }

    public class GoogleTransitVehicle
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
    
    public class GoogleTransitStop
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class GoogleTextValue
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
    
    public class GooglePolyline
    {
        [JsonPropertyName("points")]
        public string Points { get; set; } = string.Empty;
    }
}
