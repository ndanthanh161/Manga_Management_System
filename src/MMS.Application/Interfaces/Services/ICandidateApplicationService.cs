using MMS.Application.Common;
using MMS.Application.DTOs.CandidateApplication;

namespace MMS.Application.Interfaces.Services;

public interface ICandidateApplicationService
{
    Task<Result<CandidateApplicationDto>> SubmitApplicationAsync(
        SubmitApplicationRequest request, CancellationToken ct = default);
    Task<Result<CandidateApplicationDto>> ReviewApplicationAsync(
        ReviewApplicationRequest request, Guid adminUserId);
    Task<Result<IEnumerable<CandidateApplicationDto>>> GetPendingApplicationsAsync();
    Task<Result<CandidateApplicationDto>> GetApplicationByIdAsync(Guid id);
}
