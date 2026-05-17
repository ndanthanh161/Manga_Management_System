using MMS.Application.DTOs.User;
using MMS.Application.Interfaces.Services;
using MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class UserMutations
{
    public async Task<MMS.Application.Common.Result<UserDto>> CreateUser(
        [Service] IUserService userService,
        CreateUserRequest request)
    {
        return await userService.CreateUserAsync(request);
    }

    public async Task<MMS.Application.Common.Result<UserDto>> UpdateUser(
        [Service] IUserService userService,
        UpdateUserRequest request)
    {
        return await userService.UpdateUserAsync(request);
    }

    public async Task<MMS.Application.Common.Result<bool>> DeleteUser(
        [Service] IUserService userService,
        Guid id)
    {
        return await userService.DeleteUserAsync(id);
    }
}
