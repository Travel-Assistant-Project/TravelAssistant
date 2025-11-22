using SmartTripApi.DTOs;

namespace SmartTripApi.Services.AI
{
    public class PromptBuilder
    {
        public string BuildRoutePlanPrompt(RoutePlanRequestDto request)
        {
            return $@"Generate a detailed travel route plan in JSON format for {request.Region}.

Trip Parameters:
- Region: {request.Region}
- Duration: {request.Days} days
- Theme: {request.GetThemeString()}
- Budget Level: {request.GetBudgetString()}
- Activity Intensity: {request.GetIntensityString()}
- Transportation Mode: {request.GetTransportString()}

Please respond ONLY with a valid JSON object in this EXACT format:
{{
  ""PlanName"": ""Memorable trip name for {request.Region}"",
  ""Days"": [
    {{
      ""DayNumber"": 1,
      ""Activities"": [
        {{
          ""Title"": ""Activity title"",
          ""Description"": ""Detailed description of the activity"",
          ""Reason"": ""Why this activity is recommended for this trip"",
          ""StartTime"": ""09:00"",
          ""EndTime"": ""11:00"",
          ""Place"": {{
            ""Name"": ""Name of the place/location"",
            ""Description"": ""Description of the place"",
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
- Include 3-4 activities per day based on {request.GetIntensityString()} intensity
- Focus on {request.GetThemeString()} themed activities
- Consider {request.GetBudgetString()} budget level
- Plan transportation using {request.GetTransportString()}
- Provide realistic time slots (HH:mm format)
- Include specific place information for each activity
- Make sure ALL JSON is valid and properly formatted
- Do NOT include any markdown formatting or code blocks, ONLY the JSON object";
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
