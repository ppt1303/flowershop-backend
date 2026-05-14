using System.Security.Claims;

namespace FlowerShop.Common
{
    public static class UserClaimsHelper
    {
        public static int? GetUserId(ClaimsPrincipal user)
        {
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("nameid")?.Value
                ?? user.FindFirst("sub")?.Value;

            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
