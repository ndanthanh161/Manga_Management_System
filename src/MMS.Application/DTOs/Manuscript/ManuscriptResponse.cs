namespace MMS.Application.DTOs.Manuscript;

public record ManuscriptResponse(
    Guid Id,
    string Name,
    string Status
);
