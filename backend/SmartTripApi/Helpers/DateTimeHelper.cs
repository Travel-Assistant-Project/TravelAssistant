namespace SmartTripApi.Helpers
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Returns current time in Turkey timezone (UTC+3)
        /// Works on both Windows and Linux/Mac
        /// </summary>
        public static DateTime GetTurkeyTime()
        {
            try
            {
                // Try Windows timezone first
                var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
            }
            catch
            {
                // Fallback for Linux/Mac - use UTC+3 offset
                return DateTime.UtcNow.AddHours(3);
            }
        }

        /// <summary>
        /// Converts UTC DateTime to Turkey timezone (UTC+3)
        /// </summary>
        public static DateTime ToTurkeyTime(DateTime utcDateTime)
        {
            try
            {
                var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, turkeyTimeZone);
            }
            catch
            {
                // Fallback for Linux/Mac - use UTC+3 offset
                return utcDateTime.AddHours(3);
            }
        }
    }
}

