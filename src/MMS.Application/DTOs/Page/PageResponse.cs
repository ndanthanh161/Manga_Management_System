namespace MMS.Application.DTOs.Page;

public record PageResponse(
    Guid Id,
    int PageNumber,
    string ImageUrl,
    Guid ChapterId,
    int Version,
    bool IsCurrentVersion,
    string? CloudinaryPublicId,
    int Width,
    int Height,
    long FileSizeBytes,
    DateTime CreatedAt
);
