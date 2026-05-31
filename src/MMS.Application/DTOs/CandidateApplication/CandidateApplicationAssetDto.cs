namespace MMS.Application.DTOs.CandidateApplication;

public record CandidateApplicationAssetDto(
    Guid Id,
    string Name,
    string Type,
    string FileUrl,
    string PublicId,
    long Size,
    DateTime UploadedAt
);
