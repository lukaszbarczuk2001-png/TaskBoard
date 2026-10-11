using TaskBoard.Core.Enums;

namespace TaskBoard.Web.Services;

// Shapes of the JSON returned by TaskBoard.Api. Enums are shared from TaskBoard.Core
// and travel over the wire as strings ("InProgress").

public record ProjectDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAtUtc);

public record IssueDto(
    Guid Id,
    Guid ProjectId,
    int Number,
    string Title,
    string? Description,
    IssueStatus Status,
    IssuePriority Priority,
    IssueType Type,
    Guid ReporterId,
    Guid? AssigneeId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record CreateProjectRequest(string Key, string Name, string? Description);

public record CreateIssueRequest(
    string Title,
    string? Description,
    IssuePriority? Priority,
    IssueType? Type,
    Guid? AssigneeId = null);

public record UpdateIssueRequest(
    string Title,
    string? Description,
    IssuePriority Priority,
    IssueType Type,
    Guid? AssigneeId);

public record UserDto(Guid Id, string Email, string DisplayName);

public record CommentDto(Guid Id, Guid AuthorId, string AuthorName, string Content, DateTime CreatedAtUtc);

public record CreateCommentRequest(string Content);

/// <summary>Outcome of an API call: either a value or a human-readable error.</summary>
public record ApiResult<T>(T? Value, string? Error)
{
    public bool Ok => Error is null;
    public static ApiResult<T> Success(T value) => new(value, null);
    public static ApiResult<T> Failure(string error) => new(default, error);
}
