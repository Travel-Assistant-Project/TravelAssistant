using SmartTripApi.Models;

namespace SmartTripApi.DTOs
{
    // =======================
    // REQUEST DTO
    // =======================
    public class RoutePlanRequestDto
    {
        public string Region { get; set; } = string.Empty;
        public int Days { get; set; }

        // SINGLE VALUE
        public int? Theme { get; set; }
        public int? Budget { get; set; }
        public int? Intensity { get; set; }
        public int? Transport { get; set; }

        // MULTIPLE VALUES
        public List<int>? Themes { get; set; }
        public List<int>? Budgets { get; set; }
        public List<int>? Intensities { get; set; }
        public List<int>? Transports { get; set; }


        // ---- EFFECTIVE VALUE METHODS ----
        private List<int> GetEffectiveThemes() =>
            Themes?.Any() == true ? Themes! :
            Theme.HasValue ? new() { Theme.Value } :
            new();

        private List<int> GetEffectiveBudgets() =>
            Budgets?.Any() == true ? Budgets! :
            Budget.HasValue ? new() { Budget.Value } :
            new();

        private List<int> GetEffectiveIntensities() =>
            Intensities?.Any() == true ? Intensities! :
            Intensity.HasValue ? new() { Intensity.Value } :
            new();

        private List<int> GetEffectiveTransports() =>
            Transports?.Any() == true ? Transports! :
            Transport.HasValue ? new() { Transport.Value } :
            new();


        // ---- ENUM LISTS ----
        public List<ThemeType> GetThemeTypes() =>
            GetEffectiveThemes().Select(t => (ThemeType)t).ToList();

        public List<BudgetLevel> GetBudgetLevels() =>
            GetEffectiveBudgets().Select(b => (BudgetLevel)b).ToList();

        public List<IntensityLevel> GetIntensityLevels() =>
            GetEffectiveIntensities().Select(i => (IntensityLevel)i).ToList();

        public List<TransportMode> GetTransportModes() =>
            GetEffectiveTransports().Select(t => (TransportMode)t).ToList();


        // ---- LOWER STRING LISTS ----
        public List<string> GetThemeStrings() =>
            GetThemeTypes().Select(t => t.ToString().ToLower()).ToList();

        public List<string> GetBudgetStrings() =>
            GetBudgetLevels().Select(b => b.ToString().ToLower()).ToList();

        public List<string> GetIntensityStrings() =>
            GetIntensityLevels().Select(i => i.ToString().ToLower()).ToList();

        public List<string> GetTransportStrings() =>
            GetTransportModes().Select(t => t switch
            {
                TransportMode.Car => "car",
                TransportMode.Walk => "walk",
                TransportMode.PublicTransport => "public_transport",
                _ => "car"
            }).ToList();


        // ---- PRIMARY VALUES ----
        public ThemeType? GetPrimaryTheme()
        {
            var list = GetEffectiveThemes();
            return list.Any() ? (ThemeType)list[0] : null;
        }

        public BudgetLevel? GetPrimaryBudget()
        {
            var list = GetEffectiveBudgets();
            return list.Any() ? (BudgetLevel)list[0] : null;
        }

        public IntensityLevel? GetPrimaryIntensity()
        {
            var list = GetEffectiveIntensities();
            return list.Any() ? (IntensityLevel)list[0] : null;
        }

        public TransportMode? GetPrimaryTransport()
        {
            var list = GetEffectiveTransports();
            return list.Any() ? (TransportMode)list[0] : null;
        }
    }


    // =======================
    // RESPONSE DTO
    // =======================
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


    // =======================
    // AI RESPONSE DTO
    // =======================
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


    // =======================
    // USER ROUTE SUMMARY DTO
    // =======================
    public class UserRouteSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Region { get; set; } = "";
        public int DaysCount { get; set; }
        public ThemeTypeEnum? Theme { get; set; }
        public BudgetLevelEnum? Budget { get; set; }
        public IntensityLevelEnum? Intensity { get; set; }
        public TransportModeEnum? Transport { get; set; }
        public bool IsAiGenerated { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
