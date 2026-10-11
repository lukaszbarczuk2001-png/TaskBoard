using TaskBoard.Core.Enums;

namespace TaskBoard.Api.Contracts;

public record CreateIssueRequest(
    string Title,
    string? Description,
    IssuePriority? Priority,
    IssueType? Type,
    Guid? AssigneeId);

public record UpdateIssueRequest(
    string Title,
    string? Description,
    IssuePriority Priority,
    IssueType Type,
    Guid? AssigneeId);

public record UpdateIssueStatusRequest(IssueStatus Status);
