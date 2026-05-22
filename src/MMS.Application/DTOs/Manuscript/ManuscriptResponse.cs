namespace MMS.Application.DTOs.Manuscript;

public record ManuscriptResponse(
    Guid Id,
    string Name,
    string Status,
    int Version,
    bool IsCurrentVersion,
    Guid? ParentManuscriptId,
    Guid ChapterId,
    DateTime CreatedAt
);
