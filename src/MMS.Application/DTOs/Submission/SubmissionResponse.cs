using MMS.Application.DTOs.BoardVote;
using MMS.Application.DTOs.Manuscript;

namespace MMS.Application.DTOs.Submission;

public record SubmissionResponse(
    Guid Id,
    string Title,
    string Note,
    string Status,
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesDescription,
    string? SeriesCoverImageUrl,
    string? SeriesStatus,
    Guid? MangakaId,
    string MangakaName,
    Guid? AssignedTantouId,
    string? AssignedTantouName,
    DateTime SubmittedAt,
    DateTime? ResolvedAt,
    List<ManuscriptResponse> Manuscripts,
    List<BoardVoteResponse> Votes,
    List<SubmissionTransitionResponse> Transitions
);
