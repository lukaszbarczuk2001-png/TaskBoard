using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Core.Entities;
using TaskBoard.Infrastructure;

namespace TaskBoard.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var projects = app.MapGroup("/api/projects")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
                .RequireAuthenticatedUser());

        projects.MapGet("/", async (TaskBoardDbContext db) =>
            await db.Projects.AsNoTracking().ToListAsync());

        projects.MapGet("/{id:guid}", async (Guid id, TaskBoardDbContext db) =>
            await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
                is { } project
                ? Results.Ok(project)
                : Results.NotFound());

        projects.MapPost("/", async (Project project, TaskBoardDbContext db) =>
        {
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/api/projects/{project.Id}", project);
        });
    }
}
