using MMS.Application.Common;
using MMS.Application.DTOs.CandidateApplication;
using MMS.Domain.Enums;

namespace MMS.Application.Interfaces.Services;

public interface ICandidateApplicationService
{
    Task<Result<CandidateApplicationDto>> SubmitApplicationAsync(
        SubmitApplicationRequest request, CancellationToken ct = default);
    Task<Result<CandidateApplicationDto>> VoteApplicationAsync(
        Guid applicationId, Guid boardMemberId, VoteType vote, Guid? proposedTantouId, string? comment);
    Task<Result<IEnumerable<CandidateApplicationDto>>> GetPendingApplicationsAsync();
    Task<Result<CandidateApplicationDto>> GetApplicationByIdAsync(Guid id);
}
