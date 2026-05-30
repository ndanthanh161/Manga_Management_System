namespace MMS.Application.DTOs.Series;

public record SeriesResponse(
    Guid Id,
    string Title,
    string? AlternativeTitle,
    string Description,
    string? CoverImageUrl,
    string Status,
    Guid AuthorId,
    string AuthorName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    System.Collections.Generic.List<SeriesVoteDto>? Votes = null
);
