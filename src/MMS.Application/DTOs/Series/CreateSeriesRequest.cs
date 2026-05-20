namespace MMS.Application.DTOs.Series;

public record CreateSeriesRequest(
    string Title,
    string? AlternativeTitle,
    string Description,
    string? CoverImageUrl,
    string? AuthorEmail = null,
    string? AuthorName = null
);
