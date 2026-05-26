namespace MMS.Application.DTOs.CandidateApplication;

public record ReviewApplicationRequest(
    Guid ApplicationId,
    bool IsApproved,
    string? ReviewNote,
    Guid? TantouEditorId
);
