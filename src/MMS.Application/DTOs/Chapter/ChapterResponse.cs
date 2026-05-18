namespace MMS.Application.DTOs.Chapter;

public record ChapterResponse(
    Guid Id,
    string Title,
    double ChapterNumber,
    bool IsPublished,
    DateTime CreatedAt,
    Guid SeriesId
);
