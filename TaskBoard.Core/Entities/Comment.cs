namespace TaskBoard.Core.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid IssueId { get; set; }

    public Issue? Issue { get; set; }

    public Guid AuthorId { get; set; }

    public required string Content { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
