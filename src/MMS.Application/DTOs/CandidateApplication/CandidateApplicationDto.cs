namespace MMS.Application.DTOs.CandidateApplication;

public record CandidateApplicationDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SeriesTitle { get; init; } = string.Empty;
    public List<CandidateApplicationAssetDto> Assets { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public string? ReviewNote { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
}
