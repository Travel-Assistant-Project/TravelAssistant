using SmartTripApi.DTOs;

namespace SmartTripApi.Services.AI
{
    public class PromptBuilder
    {
        public string BuildTripPlanPrompt(TripPlanRequest request)
        {
            return $@"Generate a detailed travel plan in JSON format.
Region: {request.Region}
Days: {request.Days}
Theme: {request.Theme}
Budget: {request.Budget}
Intensity: {request.Intensity}
Transport: {request.Transport}

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
    }
}
