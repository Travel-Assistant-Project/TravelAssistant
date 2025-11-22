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
}
