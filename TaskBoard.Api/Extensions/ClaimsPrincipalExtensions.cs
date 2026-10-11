using System.Security.Claims;

namespace TaskBoard.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the authenticated user's id (Identity's NameIdentifier claim) as a Guid.
    /// Throws if called on a principal that isn't authenticated.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawId, out var userId)
            ? userId
            : throw new InvalidOperationException("The current user has no valid id claim.");
    }
}
