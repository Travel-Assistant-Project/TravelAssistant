using SmartTripApi.DTOs;

namespace SmartTripApi.Services.AI
{
  public class PromptBuilder
  {
    public string BuildRoutePlanPrompt(RoutePlanRequestDto request)
    {
      // Build comma-separated lists from arrays
      var themes = string.Join(", ", request.GetThemeStrings());
      var budgets = string.Join(", ", request.GetBudgetStrings());
      var intensities = string.Join(", ", request.GetIntensityStrings());
      var transports = string.Join(", ", request.GetTransportStrings());

      // If no selections, provide defaults
      if (string.IsNullOrEmpty(themes)) themes = "general sightseeing";
      if (string.IsNullOrEmpty(budgets)) budgets = "medium";
      if (string.IsNullOrEmpty(intensities)) intensities = "relaxed";
      if (string.IsNullOrEmpty(transports)) transports = "car";

      return $@"Generate a detailed travel route plan in JSON format for {request.Region}.

Trip Parameters:
- Region: {request.Region}
- Duration: {request.Days} days
- Themes: {themes}
- Budget Level: {budgets}
- Activity Intensity: {intensities}
- Transportation Modes: {transports}

IMPORTANT LOCATION RULE:
All activities and places MUST be inside {request.Region}.
Do NOT use places from other cities.
Always set City = ""{request.Region}"" and Country = ""Turkey"".

PLACE RULES:
- Use REAL, verifiable places (POIs) that exist in {request.Region}.
- Do NOT invent place names.
- If unsure, choose a general but real landmark or area.
- Place.Name must be short and clear (max ~60 characters).

TRANSPORT LOGIC (SHORT):
If only ""walk"" is available, keep activities in the same area. If ""car"" or ""public_transport"" is available, city-wide distances are acceptable.

THEME BALANCE:
When multiple themes are provided, distribute activities across all selected themes.

Please respond ONLY with a valid JSON object in this EXACT format:
{{
  ""PlanName"": ""Memorable trip in {request.Region}"",
  ""Days"": [
    {{
      ""DayNumber"": 1,
      ""Activities"": [
        {{
          ""Title"": ""Activity title"",
          ""Description"": ""Detailed description"",
          ""Reason"": ""Why this activity fits the trip (mention the theme)"",
          ""StartTime"": ""09:00"",
          ""EndTime"": ""11:00"",
          ""Place"": {{
            ""Name"": ""Place name"",
            ""Description"": ""Place description"",
            ""City"": ""{request.Region}"",
            ""Country"": ""Turkey""
          }}
        }}
      ]
    }}
  ]
}}

REQUIREMENTS:
- Generate exactly {request.Days} days
- Provide realistic time slots (HH:mm format)
- Keep Description, Reason, and Place.Description short (1 sentence each).
- Include exactly 3 activities per day.
- Use clear and concise language; avoid overly long or story-like descriptions.
- Return ONLY valid JSON (no markdown, no extra text)";
    }

    public string BuildTripPlanPrompt(TripPlanRequest request)
    {
      return $@"Generate a detailed travel plan in JSON format.
Region: {request.Region}
Days: {request.Days}
Theme: {request.Theme.ToString().ToLower()}
Budget: {request.Budget.ToString().ToLower()}
Intensity: {request.Intensity.ToString().ToLower()}
Transport: {GetTransportString(request.Transport)}

Please respond ONLY with a valid JSON object in this exact format:
{{
  ""PlanName"": ""Trip to {request.Region}"",
  ""Days"": [
    {{
      ""DayNumber"": 1,
      ""Activities"": [
        {{
          ""Title"": ""Activity name"",
          ""Description"": ""Activity description"",
          ""Reason"": ""Why this activity is recommended"",
          ""StartTime"": ""09:00"",
          ""EndTime"": ""11:00""
        }}
      ]
    }}
  ]
}}

Generate {request.Days} days with 3-4 activities per day. Make sure the JSON is valid and complete.";
    }

    private string GetTransportString(TransportMode transport)
    {
      return transport switch
      {
        TransportMode.PublicTransport => "public_transport",
        TransportMode.Car => "car",
        TransportMode.Walk => "walk",
        _ => transport.ToString().ToLower()
      };
    }
  }
}
