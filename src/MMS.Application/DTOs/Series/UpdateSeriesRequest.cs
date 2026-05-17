using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Series;

public record UpdateSeriesRequest(
    string? Title,
    string? AlternativeTitle,
    string? Description,
    string? CoverImageUrl,
    SeriesStatus? Status
);
