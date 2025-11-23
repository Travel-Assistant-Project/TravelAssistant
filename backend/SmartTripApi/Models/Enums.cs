namespace SmartTripApi.Models
{
    // PostgreSQL enum mappings
    public enum BudgetLevelEnum
    {
        low,
        medium,
        high
    }

    public enum IntensityLevelEnum
    {
        relaxed,
        active
    }

    public enum TransportModeEnum
    {
        car,
        walk,
        public_transport
    }

    public enum ThemeTypeEnum
    {
        nature,
        sea,
        history,
        beach,
        food,
        photospot
    }

    public enum UserRoleEnum
    {
        user,
        admin
    }

    // DTO enums (for frontend mapping, 0-based indexing)
    public enum ThemeType
    {
        Nature = 0,
        Sea = 1,
        History = 2,
        Beach = 3,
        Food = 4,
        Photospot = 5
    }

    public enum BudgetLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum IntensityLevel
    {
        Relaxed = 0,
        Active = 1
    }

    public enum TransportMode
    {
        Car = 0,
        Walk = 1,
        PublicTransport = 2
    }
}
