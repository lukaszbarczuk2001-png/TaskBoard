namespace TaskBoard.Api.Contracts;

public record CreateProjectRequest(string Key, string Name, string? Description);
