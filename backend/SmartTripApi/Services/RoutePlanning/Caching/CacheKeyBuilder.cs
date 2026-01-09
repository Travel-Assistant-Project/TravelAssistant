using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SmartTripApi.DTOs;

namespace SmartTripApi.Services.RoutePlanning.Caching
{
    public interface ICacheKeyBuilder
    {
        (string cacheKey, string requestHash) Build(int userId, RoutePlanRequestDto request);
    }

    public class CacheKeyBuilder : ICacheKeyBuilder
    {
        // versiyonlar değişirse cacheKey de değişsin
        private const string PromptVersion = "v1";
        private const string SchemaVersion = "routeplan_v1";
        private const string ModelName = "gemini-2.5-flash";

        public (string cacheKey, string requestHash) Build(int userId, RoutePlanRequestDto request)
        {
            var requestOnly = new
            {
                userId,
                region = (request.Region ?? "").Trim().ToLowerInvariant(),
                days = request.Days,
                themes = request.GetThemeStrings()?.Select(x => x.Trim().ToLowerInvariant()).OrderBy(x => x).ToArray(),
                budgets = request.GetBudgetStrings()?.Select(x => x.Trim().ToLowerInvariant()).OrderBy(x => x).ToArray(),
                intensities = request.GetIntensityStrings()?.Select(x => x.Trim().ToLowerInvariant()).OrderBy(x => x).ToArray(),
                transports = request.GetTransportStrings()?.Select(x => x.Trim().ToLowerInvariant()).OrderBy(x => x).ToArray()
            };

            var requestHash = Sha256Hex(JsonSerializer.Serialize(requestOnly));

            var cachePayload = new
            {
                request = requestOnly,
                promptVersion = PromptVersion,
                schemaVersion = SchemaVersion,
                modelName = ModelName
            };

            var cacheKey = "routeplan:" + Sha256Hex(JsonSerializer.Serialize(cachePayload));
            return (cacheKey, requestHash);
        }

        private static string Sha256Hex(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
