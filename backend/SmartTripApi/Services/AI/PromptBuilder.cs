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
- Themes: {themes} (can mix different themes across activities)
- Budget Level: {budgets}
- Activity Intensity: {intensities} (can vary throughout the trip)
- Transportation Modes: {transports} (can use different modes for different activities)

IMPORTANT: When multiple themes are provided, create a diverse itinerary that includes activities from all specified themes. 
For example, if themes are 'nature, beach, history', include some nature activities, some beach time, and some historical sites.

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
          ""Reason"": ""Why this activity is recommended for this trip (mention which theme it addresses)"",
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
- Include 3-4 activities per day based on the specified intensity level(s)
- DIVERSIFY activities across all selected themes: {themes}
- Consider the budget level(s): {budgets}
- Plan transportation using available mode(s): {transports}
- Provide realistic time slots (HH:mm format)
- Include specific place information for each activity
- Make sure ALL JSON is valid and properly formatted
- Do NOT include any markdown formatting or code blocks, ONLY the JSON object
- IMPORTANT: Balance activities across different themes throughout the days";
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
