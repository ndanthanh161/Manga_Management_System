using MMS.Application.DTOs.User;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;
using R = MMS.Application.Common;

using MMS.API.Authorization;

namespace MMS.API.GraphQL;

public class UserQueries
{
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<UserDto>> GetUsers([Service] IUserService userService)
    {
        var result = await userService.GetAllUsersAsync();
        return result.Succeeded ? result.Data! : Enumerable.Empty<UserDto>();
    }

    [Authorize]
    public async Task<R.Result<UserDto>> GetUserById([Service] IUserService userService, Guid id)
    {
        return await userService.GetUserByIdAsync(id);
    }

    [Authorize(Policy = PolicyNames.CanAssignAssistantTasks)]
    public async Task<IEnumerable<UserDto>> GetMyAssistants(
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await userService.GetAssistantsByMangakaAsync(mangakaId);
        return result.Succeeded ? result.Data! : Enumerable.Empty<UserDto>();
    }
}
