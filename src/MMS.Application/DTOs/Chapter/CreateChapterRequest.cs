namespace MMS.Application.DTOs.Chapter;

public record CreateChapterRequest(
    string Title,
    double ChapterNumber,
    Guid SeriesId
);
