using System.Security.Claims;

namespace F1Predictions.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the user ID from the JWT claims.
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst("id");
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token claims.");
            }
            return userId;
        }

        /// <summary>
        /// Tries to get the user ID from the JWT claims.
        /// </summary>
        public static bool TryGetUserId(this ClaimsPrincipal user, out int userId)
        {
            userId = 0;
            var idClaim = user.FindFirst("id");
            return idClaim != null && int.TryParse(idClaim.Value, out userId);
        }
    }
}
