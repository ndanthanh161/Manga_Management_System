using MMS.Application.DTOs.Task;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class TaskMutations
{
    [Authorize(Roles = new[] { "Mangaka" })]
    public async Task<TaskResponse> AssignTask(
        [Service] ITaskService taskService,
        ClaimsPrincipal claimsPrincipal,
        AssignTaskRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await taskService.AssignTaskAsync(mangakaId, input);
    }

    [Authorize(Roles = new[] { "Mangaka", "Assistant" })]
    public async Task<TaskResponse> UpdateTaskStatus(
        [Service] ITaskService taskService,
        ClaimsPrincipal claimsPrincipal,
        Guid taskId,
        UpdateTaskStatusRequest input)
    {
        var requesterId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await taskService.UpdateStatusAsync(taskId, requesterId, input);
    }
}
