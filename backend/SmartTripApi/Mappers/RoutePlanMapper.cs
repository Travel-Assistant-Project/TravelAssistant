using System.Text.Json;
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
                                StartTime = a.StartTime.HasValue 
                                    ? $"{a.StartTime.Value.Hours:D2}:{a.StartTime.Value.Minutes:D2}" 
                                    : "",
                                EndTime = a.EndTime.HasValue 
                                    ? $"{a.EndTime.Value.Hours:D2}:{a.EndTime.Value.Minutes:D2}" 
                                    : "",
                                Place = a.Place != null
                                    ? new PlaceDetailDto
                                    {
                                        Id = a.Place.Id,  // 👈 PLACE ID EKLENDİ
                                        Name = a.Place.Name,
                                        Description = a.Place.Description,
                                        City = a.Place.City,
                                        Country = a.Place.Country,
                                        ImageUrls = a.Place.PhotoUrls != null && a.Place.PhotoUrls.Length > 0
                                            ? a.Place.PhotoUrls.ToList()
                                            : null,
                                        GoogleRating = a.Place.GoogleRating,
                                        Latitude = a.Place.Latitude,
                                        Longitude = a.Place.Longitude
                                    }
                                    : null,
                                // Prefer new Transport table, fallback to legacy fields
                                TravelFromPreviousMode = a.Transport?.Mode ?? a.TravelFromPreviousMode,
                                TravelFromPreviousDurationMinutes = a.Transport?.DurationMinutes ?? a.TravelFromPreviousDurationMinutes,
                                TravelFromPreviousDistanceMeters = a.Transport?.DistanceMeters ?? a.TravelFromPreviousDistanceMeters,
                                TravelFromPreviousDetails = !string.IsNullOrEmpty(a.Transport?.DetailsJson)
                                    ? JsonSerializer.Deserialize<object>(a.Transport.DetailsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                                    : (!string.IsNullOrEmpty(a.TravelFromPreviousDetailsJson) 
                                        ? JsonSerializer.Deserialize<object>(a.TravelFromPreviousDetailsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                                        : null),
                                TravelFromPreviousPolyline = a.Transport?.PolylinePoints ?? a.TravelFromPreviousPolyline
                            })
                            .ToList(),
                        WeatherInfo = day.WeatherInfo != null 
                            ? JsonSerializer.Deserialize<WeatherInfoDto>(day.WeatherInfo.RootElement.GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            })
                            : null
                    })
                    .ToList()
            };
        }
    }
}
