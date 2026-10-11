using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Extensions;
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
            await db.Projects.AsNoTracking().OrderBy(p => p.Key).ToListAsync());

        projects.MapGet("/{id:guid}", async (Guid id, TaskBoardDbContext db) =>
            await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
                is { } project
                ? Results.Ok(project)
                : Results.NotFound());

        projects.MapPost("/", async (
            CreateProjectRequest request,
            TaskBoardDbContext db,
            ClaimsPrincipal user) =>
        {
            var key = request.Key.Trim().ToUpperInvariant();

            if (key.Length is < 2 or > 10 || !key.All(char.IsLetterOrDigit))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["key"] = ["Key must be 2-10 letters/digits."]
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Name is required."]
                });
            }

            if (await db.Projects.AnyAsync(p => p.Key == key))
            {
                return Results.Conflict(new { error = $"A project with key '{key}' already exists." });
            }

            var project = new Project
            {
                Key = key,
                Name = request.Name.Trim(),
                Description = request.Description,
                OwnerId = user.GetUserId()
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/api/projects/{project.Id}", project);
        });
    }
}
