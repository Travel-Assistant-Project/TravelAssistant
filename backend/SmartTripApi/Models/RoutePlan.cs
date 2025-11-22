using System;

namespace SmartTripApi.Data
{
    public class RoutePlan
    {
        public int Id { get; set; }

        // Request’ten gelen özet bilgiler
        public string Region { get; set; } = string.Empty;
        public int Days { get; set; }
        public string Theme { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string Intensity { get; set; } = string.Empty;
        public string Transport { get; set; } = string.Empty;

        // AI’den gelen plan bilgileri
        public string PlanName { get; set; } = string.Empty;

        // Tüm TripPlanResponse’u JSON olarak saklıyoruz (postgres’te jsonb’ye çevirebilirsin)
        public string PlanJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
