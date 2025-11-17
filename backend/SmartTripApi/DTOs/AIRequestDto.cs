namespace SmartTripApi.DTOs
{
    public class TripPlanRequest
    {
        public string Region { get; set; } = string.Empty;
        public int Days { get; set; }
        public string Theme { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string Intensity { get; set; } = string.Empty;
        public string Transport { get; set; } = string.Empty;
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
