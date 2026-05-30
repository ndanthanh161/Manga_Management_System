using MMS.Application.DTOs.CandidateApplication;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using R = MMS.Application.Common;

using MMS.API.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class CandidateApplicationQueries
{
    /// <summary>
    /// Admin xem danh sách đơn ứng tuyển đang chờ duyệt
    /// </summary>
    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<R.Result<IEnumerable<CandidateApplicationDto>>> GetPendingApplications(
        [Service] ICandidateApplicationService service)
    {
        return await service.GetPendingApplicationsAsync();
    }

    /// <summary>
    /// Admin xem chi tiết một đơn ứng tuyển
    /// </summary>
    [Authorize(Policy = PolicyNames.CanVoteInSession)]
    public async Task<R.Result<CandidateApplicationDto>> GetApplicationById(
        [Service] ICandidateApplicationService service,
        Guid id)
    {
        return await service.GetApplicationByIdAsync(id);
    }
}
