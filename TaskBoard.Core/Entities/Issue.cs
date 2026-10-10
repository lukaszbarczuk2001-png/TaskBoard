using TaskBoard.Core.Enums;

namespace TaskBoard.Core.Entities;

public class Issue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Sequential number within the project, used to build "TB-123".</summary>
    public int Number { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public IssueStatus Status { get; set; } = IssueStatus.ToDo;

    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    public IssueType Type { get; set; } = IssueType.Task;

    public Guid ReporterId { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
