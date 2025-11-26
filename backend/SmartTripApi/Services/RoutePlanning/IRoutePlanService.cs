using SmartTripApi.DTOs;

namespace SmartTripApi.Services.RoutePlanning
{
    public interface IRoutePlanService
    {
        Task<ServiceResult<RoutePlanResponseDto>> CreateRoutePlanAsync(int userId, RoutePlanRequestDto request);

        Task<RoutePlanResponseDto?> GetRoutePlanAsync(int userId, int itineraryId);

        Task<List<UserRouteSummaryDto>> GetUserRoutePlansAsync(int userId);
    }
}
