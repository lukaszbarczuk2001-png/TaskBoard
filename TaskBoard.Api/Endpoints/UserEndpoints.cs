using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Contracts;
using TaskBoard.Infrastructure;

namespace TaskBoard.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
                .RequireAuthenticatedUser());

        // Everyone signed in can see everyone else (used for the assignee picker).
        // Falls back to the email as the display name until users can set one.
        users.MapGet("/", async (TaskBoardDbContext db) =>
            await db.Users.AsNoTracking()
                .OrderBy(u => u.Email)
                .Select(u => new UserSummary(
                    u.Id,
                    u.Email!,
                    u.DisplayName != "" ? u.DisplayName : u.Email!))
                .ToListAsync());
    }
}
