namespace MMS.Application.DTOs.Series;

public record SeriesRankingResponse(
    Guid SeriesId,
    string SeriesTitle,
    int Position,
    string Period,
    DateTime CalculatedAt
);
