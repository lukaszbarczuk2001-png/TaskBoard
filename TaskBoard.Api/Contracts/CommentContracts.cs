namespace TaskBoard.Api.Contracts;

public record CreateCommentRequest(string Content);

public record CommentResponse(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Content,
    DateTime CreatedAtUtc);
