using MMS.Application.Interfaces.Services;
using MMS.Application.DTOs;
using MMS.Application.Common;

namespace MMS.API.GraphQL;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<UserDto>> GetUsers([Service] IUserService userService) 
    {
        var result = await userService.GetAllUsersAsync();
        return result.Succeeded ? result.Data! : Enumerable.Empty<UserDto>();
    }

    public async Task<MMS.Application.Common.Result<UserDto>> GetUserById([Service] IUserService userService, Guid id)
    {
        return await userService.GetUserByIdAsync(id);
    }
}
