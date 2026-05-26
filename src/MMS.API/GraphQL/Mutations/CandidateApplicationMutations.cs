using MMS.Application.DTOs.CandidateApplication;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;
using R = MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class CandidateApplicationMutations
{
    /// <summary>
    /// Admin duyệt hoặc từ chối đơn ứng tuyển
    /// </summary>
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<R.Result<CandidateApplicationDto>> ReviewApplication(
        [Service] ICandidateApplicationService service,
        ReviewApplicationRequest request,
        ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return R.Result<CandidateApplicationDto>.Failure("UNAUTHORIZED", "Unauthorized", 401);

        return await service.ReviewApplicationAsync(request, Guid.Parse(userIdString));
    }
}
