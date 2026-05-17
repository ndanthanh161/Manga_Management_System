using MMS.Application.DTOs.Manuscript;

namespace MMS.Application.DTOs.Submission;

public record CreateSubmissionRequest(
    Guid SeriesId,
    string Title,
    string Note,
    List<CreateManuscriptRequest> Manuscripts
);
