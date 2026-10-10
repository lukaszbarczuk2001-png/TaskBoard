namespace TaskBoard.Core.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Short code used as an issue prefix, e.g. "TB" for TB-123.</summary>
    public required string Key { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
