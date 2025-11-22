namespace SmartTripApi.DTOs
{
    public class RoutePlanRequestDto
    {
        public string Region { get; set; } = string.Empty;
        public int Days { get; set; }
        public int Theme { get; set; }        // 1-6 (Nature, Sea, History, Beach, Food, Photospot)
        public int Budget { get; set; }       // 1-3 (Low, Medium, High)
        public int Intensity { get; set; }    // 1-2 (Relaxed, Active)
        public int Transport { get; set; }    // 1-3 (Car, Walk, PublicTransport)

        public ThemeType GetThemeType() => (ThemeType)(Theme - 1);
        public BudgetLevel GetBudgetLevel() => (BudgetLevel)(Budget - 1);
        public IntensityLevel GetIntensityLevel() => (IntensityLevel)(Intensity - 1);
        public TransportMode GetTransportMode() => (TransportMode)(Transport - 1);
        
        public string GetThemeString() => GetThemeType().ToString().ToLower();
        public string GetBudgetString() => GetBudgetLevel().ToString().ToLower();
        public string GetIntensityString() => GetIntensityLevel().ToString().ToLower();
        public string GetTransportString() => GetTransportMode() switch
        {
            TransportMode.Car => "car",
            TransportMode.Walk => "walk",
            TransportMode.PublicTransport => "public_transport",
            _ => "car"
        };
    }

    public class RoutePlanResponseDto
    {
        public int ItineraryId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int DaysCount { get; set; }
        public List<DayDetailDto> Days { get; set; } = new();
    }

    public class DayDetailDto
    {
        public int DayNumber { get; set; }
        public List<ActivityDetailDto> Activities { get; set; } = new();
    }

    public class ActivityDetailDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public PlaceDetailDto? Place { get; set; }
    }

    public class PlaceDetailDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    // AI Response DTOs
    public class AIRoutePlanResponse
    {
        public string PlanName { get; set; } = string.Empty;
        public List<AIDayPlan> Days { get; set; } = new();
    }

    public class AIDayPlan
    {
        public int DayNumber { get; set; }
        public List<AIActivity> Activities { get; set; } = new();
    }

    public class AIActivity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public AIPlace? Place { get; set; }
    }

    public class AIPlace
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }
}
