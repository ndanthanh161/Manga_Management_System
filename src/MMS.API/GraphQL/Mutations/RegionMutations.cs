using MMS.Application.DTOs.Region;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class RegionMutations
{
    [Authorize]
    public async Task<RegionResponse> CreateRegion(
        [Service] IRegionService regionService,
        ClaimsPrincipal claimsPrincipal,
        CreateRegionRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await regionService.CreateAsync(mangakaId, input);
    }
}
