// using System.Text.Json;
using System.Text.Json;

namespace SmartTripApi.Models
{
    public class Itinerary
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int DaysCount { get; set; }
        public ThemeTypeEnum? Theme { get; set; }
        public BudgetLevelEnum? Budget { get; set; }
        public IntensityLevelEnum? Intensity { get; set; }
        public TransportModeEnum? Transport { get; set; }
        public string? SelectedTransportModes { get; set; } // JSON array of selected transport modes
        
        // Store original multi-selection parameters for recreation
        public string? OriginalThemes { get; set; } // JSON array of original theme selections
        public string? OriginalBudgets { get; set; } // JSON array of original budget selections
        public string? OriginalIntensities { get; set; } // JSON array of original intensity selections
        public string? OriginalTransports { get; set; } // JSON array of original transport selections
        
        public bool IsAiGenerated { get; set; } = false;
        public string Status { get; set; } = "pending"; // pending, processing, completed, failed
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public List<ItineraryDay> ItineraryDays { get; set; } = new();

        // Helper method to get selected transport modes from JSON
        public List<string> GetSelectedTransportModes()
        {
            if (string.IsNullOrEmpty(SelectedTransportModes))
                return new List<string>();
                
            try
            {
                return JsonSerializer.Deserialize<List<string>>(SelectedTransportModes) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        
        // Helper methods to get original selections
        public List<int> GetOriginalThemes()
        {
            if (string.IsNullOrEmpty(OriginalThemes)) return new List<int>();
            try { return JsonSerializer.Deserialize<List<int>>(OriginalThemes) ?? new List<int>(); }
            catch { return new List<int>(); }
        }
        
        public List<int> GetOriginalBudgets()
        {
            if (string.IsNullOrEmpty(OriginalBudgets)) return new List<int>();
            try { return JsonSerializer.Deserialize<List<int>>(OriginalBudgets) ?? new List<int>(); }
            catch { return new List<int>(); }
        }
        
        public List<int> GetOriginalIntensities()
        {
            if (string.IsNullOrEmpty(OriginalIntensities)) return new List<int>();
            try { return JsonSerializer.Deserialize<List<int>>(OriginalIntensities) ?? new List<int>(); }
            catch { return new List<int>(); }
        }
        
        public List<int> GetOriginalTransports()
        {
            if (string.IsNullOrEmpty(OriginalTransports)) return new List<int>();
            try { return JsonSerializer.Deserialize<List<int>>(OriginalTransports) ?? new List<int>(); }
            catch { return new List<int>(); }
        }
    }
}
