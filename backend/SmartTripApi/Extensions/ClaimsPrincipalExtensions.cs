using System.Security.Claims;

namespace SmartTripApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim))
                return null;

            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
