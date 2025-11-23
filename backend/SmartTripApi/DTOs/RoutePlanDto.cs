namespace SmartTripApi.DTOs
{
    public class RoutePlanRequestDto
    {
        public string Region { get; set; } = string.Empty;
        public int Days { get; set; }
        
        // SINGLE VALUE SUPPORT 
        public int? Theme { get; set; }        // Single theme selection (0-5)
        public int? Budget { get; set; }       // Single budget selection (0-2)
        public int? Intensity { get; set; }    // Single intensity selection (0-1)
        public int? Transport { get; set; }    // Single transport selection (0-2)
        
        // MULTIPLE VALUES SUPPORT
        public List<int>? Themes { get; set; }        // Multiple themes (0-5)
        public List<int>? Budgets { get; set; }       // Multiple budgets (0-2)
        public List<int>? Intensities { get; set; }   // Multiple intensities (0-1)
        public List<int>? Transports { get; set; }    // Multiple transports (0-2)

        // Get effective themes (either from Theme or Themes)
        private List<int> GetEffectiveThemes()
        {
            if (Themes != null && Themes.Any()) return Themes;
            if (Theme.HasValue) return new List<int> { Theme.Value };
            return new List<int>();
        }

        private List<int> GetEffectiveBudgets()
        {
            if (Budgets != null && Budgets.Any()) return Budgets;
            if (Budget.HasValue) return new List<int> { Budget.Value };
            return new List<int>();
        }

        private List<int> GetEffectiveIntensities()
        {
            if (Intensities != null && Intensities.Any()) return Intensities;
            if (Intensity.HasValue) return new List<int> { Intensity.Value };
            return new List<int>();
        }

        private List<int> GetEffectiveTransports()
        {
            if (Transports != null && Transports.Any()) return Transports;
            if (Transport.HasValue) return new List<int> { Transport.Value };
            return new List<int>();
        }

        public List<ThemeType> GetThemeTypes() => 
            GetEffectiveThemes().Select(t => (ThemeType)t).ToList();
        
        public List<BudgetLevel> GetBudgetLevels() => 
            GetEffectiveBudgets().Select(b => (BudgetLevel)b).ToList();
        
        public List<IntensityLevel> GetIntensityLevels() => 
            GetEffectiveIntensities().Select(i => (IntensityLevel)i).ToList();
        
        public List<TransportMode> GetTransportModes() => 
            GetEffectiveTransports().Select(t => (TransportMode)t).ToList();
        
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

        // Helper method to get primary theme (first in list) for database storage
        public ThemeType? GetPrimaryTheme()
        {
            var themes = GetEffectiveThemes();
            return themes.Any() ? (ThemeType)themes[0] : null;
        }
        
        public BudgetLevel? GetPrimaryBudget()
        {
            var budgets = GetEffectiveBudgets();
            return budgets.Any() ? (BudgetLevel)budgets[0] : null;
        }
        
        public IntensityLevel? GetPrimaryIntensity()
        {
            var intensities = GetEffectiveIntensities();
            return intensities.Any() ? (IntensityLevel)intensities[0] : null;
        }
        
        public TransportMode? GetPrimaryTransport()
        {
            var transports = GetEffectiveTransports();
            return transports.Any() ? (TransportMode)transports[0] : null;
        }
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
