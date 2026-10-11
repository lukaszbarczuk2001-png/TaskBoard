using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Contracts;
using TaskBoard.Api.Extensions;
using TaskBoard.Core.Entities;
using TaskBoard.Infrastructure;

namespace TaskBoard.Api.Endpoints;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var comments = app.MapGroup("/api/projects/{projectId:guid}/issues/{issueId:guid}/comments")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
                .RequireAuthenticatedUser());

        comments.MapGet("/", async (Guid projectId, Guid issueId, TaskBoardDbContext db) =>
        {
            if (!await IssueExistsAsync(db, projectId, issueId))
            {
                return Results.NotFound();
            }

            var result = await (
                from c in db.Comments.AsNoTracking()
                where c.IssueId == issueId
                join u in db.Users on c.AuthorId equals u.Id into authors
                from author in authors.DefaultIfEmpty()
                orderby c.CreatedAtUtc
                select new CommentResponse(
                    c.Id,
                    c.AuthorId,
                    author == null ? "Unknown user" : (author.DisplayName != "" ? author.DisplayName : author.Email!),
                    c.Content,
                    c.CreatedAtUtc)).ToListAsync();

            return Results.Ok(result);
        });

        comments.MapPost("/", async (
            Guid projectId,
            Guid issueId,
            CreateCommentRequest request,
            TaskBoardDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!await IssueExistsAsync(db, projectId, issueId))
            {
                return Results.NotFound();
            }

            var content = request.Content?.Trim() ?? string.Empty;
            if (content.Length is 0 or > 4000)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["content"] = ["Comment must be 1-4000 characters."]
                });
            }

            var authorId = user.GetUserId();
            var comment = new Comment { IssueId = issueId, AuthorId = authorId, Content = content };
            db.Comments.Add(comment);
            await db.SaveChangesAsync();

            var author = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == authorId);
            var authorName = author is null
                ? "Unknown user"
                : (author.DisplayName != "" ? author.DisplayName : author.Email!);

            return Results.Created(
                $"/api/projects/{projectId}/issues/{issueId}/comments/{comment.Id}",
                new CommentResponse(comment.Id, authorId, authorName, comment.Content, comment.CreatedAtUtc));
        });

        comments.MapDelete("/{commentId:guid}", async (
            Guid projectId,
            Guid issueId,
            Guid commentId,
            TaskBoardDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!await IssueExistsAsync(db, projectId, issueId))
            {
                return Results.NotFound();
            }

            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.IssueId == issueId);
            if (comment is null)
            {
                return Results.NotFound();
            }

            // Only the author may delete their own comment.
            if (comment.AuthorId != user.GetUserId())
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            db.Comments.Remove(comment);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static Task<bool> IssueExistsAsync(TaskBoardDbContext db, Guid projectId, Guid issueId) =>
        db.Issues.AnyAsync(i => i.Id == issueId && i.ProjectId == projectId);
}
