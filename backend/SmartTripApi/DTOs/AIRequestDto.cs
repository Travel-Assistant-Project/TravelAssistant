namespace SmartTripApi.DTOs
{
    public enum BudgetLevel
    {
        Low,
        Medium,
        High
    }

    public enum IntensityLevel
    {
        Relaxed,
        Active
    }

    public enum TransportMode
    {
        Car,
        Walk,
        PublicTransport
    }

    public enum ThemeType
    {
        Nature,
        Sea,
        History,
        Beach,
        Food,
        Photospot
    }

    public class TripPlanRequest
    {
        public string Region { get; set; } = string.Empty;
        public int Days { get; set; }
        public ThemeType Theme { get; set; }
        public BudgetLevel Budget { get; set; }
        public IntensityLevel Intensity { get; set; }
        public TransportMode Transport { get; set; }
    }

    public class TripPlanResponse
    {
        public string PlanName { get; set; } = string.Empty;
        public List<DayPlan> Days { get; set; } = new();
    }

    public class DayPlan
    {
        public int DayNumber { get; set; }
        public List<Activity> Activities { get; set; } = new();
    }

    public class Activity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}
