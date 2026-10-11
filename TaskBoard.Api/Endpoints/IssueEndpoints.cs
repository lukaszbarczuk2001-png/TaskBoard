using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Extensions;
using TaskBoard.Core.Entities;
using TaskBoard.Core.Enums;
using TaskBoard.Infrastructure;

namespace TaskBoard.Api.Endpoints;

public static class IssueEndpoints
{
    public static void MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        var issues = app.MapGroup("/api/projects/{projectId:guid}/issues")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
                .RequireAuthenticatedUser());

        issues.MapGet("/", async (Guid projectId, TaskBoardDbContext db) =>
            await db.Issues.AsNoTracking()
                .Where(i => i.ProjectId == projectId)
                .OrderBy(i => i.Number)
                .ToListAsync());

        issues.MapGet("/{id:guid}", async (Guid projectId, Guid id, TaskBoardDbContext db) =>
            await db.Issues.AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProjectId == projectId && i.Id == id)
                is { } issue
                ? Results.Ok(issue)
                : Results.NotFound());

        issues.MapPost("/", async (
            Guid projectId,
            CreateIssueRequest request,
            TaskBoardDbContext db,
            ClaimsPrincipal user) =>
        {
            var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists)
            {
                return Results.NotFound($"Project {projectId} not found.");
            }

            // Issue numbers are per-project (TB-1, TB-2, ...), so compute the next
            // one from what's already there instead of a global identity column.
            var nextNumber = await db.Issues
                .Where(i => i.ProjectId == projectId)
                .Select(i => (int?)i.Number)
                .MaxAsync() ?? 0;

            var issue = new Issue
            {
                ProjectId = projectId,
                Number = nextNumber + 1,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority ?? IssuePriority.Medium,
                Type = request.Type ?? IssueType.Task,
                ReporterId = user.GetUserId(),
                AssigneeId = request.AssigneeId
            };

            db.Issues.Add(issue);
            await db.SaveChangesAsync();
            return Results.Created($"/api/projects/{projectId}/issues/{issue.Id}", issue);
        });

        // Separate, narrow endpoint for status moves so the Kanban board's
        // drag-and-drop can PATCH just the one field it changes.
        issues.MapPatch("/{id:guid}/status", async (
            Guid projectId,
            Guid id,
            UpdateIssueStatusRequest request,
            TaskBoardDbContext db) =>
        {
            var issue = await db.Issues.FirstOrDefaultAsync(i => i.ProjectId == projectId && i.Id == id);
            if (issue is null)
            {
                return Results.NotFound();
            }

            issue.Status = request.Status;
            issue.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(issue);
        });

        issues.MapPut("/{id:guid}", async (
            Guid projectId,
            Guid id,
            UpdateIssueRequest request,
            TaskBoardDbContext db) =>
        {
            var issue = await db.Issues.FirstOrDefaultAsync(i => i.ProjectId == projectId && i.Id == id);
            if (issue is null)
            {
                return Results.NotFound();
            }

            issue.Title = request.Title;
            issue.Description = request.Description;
            issue.Priority = request.Priority;
            issue.Type = request.Type;
            issue.AssigneeId = request.AssigneeId;
            issue.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(issue);
        });

        issues.MapDelete("/{id:guid}", async (Guid projectId, Guid id, TaskBoardDbContext db) =>
        {
            var issue = await db.Issues.FirstOrDefaultAsync(i => i.ProjectId == projectId && i.Id == id);
            if (issue is null)
            {
                return Results.NotFound();
            }

            db.Issues.Remove(issue);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
