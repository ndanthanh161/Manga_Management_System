namespace MMS.Application.DTOs.Submission;

public record SubmitForReviewRequest(
    Guid ManuscriptId,
    string? Note
);
