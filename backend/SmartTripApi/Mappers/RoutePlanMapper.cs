using SmartTripApi.DTOs;
using SmartTripApi.Models;

namespace SmartTripApi.Mappers
{
    public static class RoutePlanMapper
    {
        public static RoutePlanResponseDto ToRoutePlanResponseDto(Itinerary itinerary)
        {
            return new RoutePlanResponseDto
            {
                ItineraryId = itinerary.Id,
                PlanName = itinerary.Name,
                Region = itinerary.Region,
                DaysCount = itinerary.DaysCount,
                Days = itinerary.ItineraryDays
                    .OrderBy(d => d.DayNumber)
                    .Select(day => new DayDetailDto
                    {
                        DayNumber = day.DayNumber,
                        Activities = day.Activities
                            .Select(a => new ActivityDetailDto
                            {
                                Title = a.Title,
                                Description = a.Description ?? "",
                                Reason = a.Reason ?? "",
                                StartTime = a.StartTime?.ToString(@"hh\\:mm") ?? "",
                                EndTime = a.EndTime?.ToString(@"hh\\:mm") ?? "",
                                Place = a.Place != null
                                    ? new PlaceDetailDto
                                    {
                                        Name = a.Place.Name,
                                        Description = a.Place.Description,
                                        City = a.Place.City,
                                        Country = a.Place.Country
                                    }
                                    : null
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}
