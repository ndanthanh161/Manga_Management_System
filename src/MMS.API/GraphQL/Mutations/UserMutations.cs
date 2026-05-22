using MMS.Application.DTOs.User;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;
using R = MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class UserMutations
{
    public async Task<R.Result<UserDto>> CreateUser(
        [Service] IUserService userService,
        CreateUserRequest request)
    {
        return await userService.CreateUserAsync(request);
    }

    public async Task<R.Result<UserDto>> UpdateUser(
        [Service] IUserService userService,
        UpdateUserRequest request)
    {
        return await userService.UpdateUserAsync(request);
    }

    public async Task<R.Result<bool>> DeleteUser(
        [Service] IUserService userService,
        Guid id)
    {
        return await userService.DeleteUserAsync(id);
    }

    [Authorize(Roles = new[] { "Mangaka" })]
    public async Task<R.Result<UserDto>> SetAssistantBaseSalary(
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid assistantId,
        decimal baseSalary)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await userService.SetAssistantBaseSalaryAsync(mangakaId, assistantId, baseSalary);
    }
}
