using MMS.Application.DTOs.Task;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class TaskQueries
{
    /// <summary>Xem các task được giao cho một user (Assistant)</summary>
    public async Task<IEnumerable<TaskResponse>> GetTasksByUser(
        [Service] ITaskService taskService,
        Guid userId)
    {
        return await taskService.GetByAssignedUserAsync(userId);
    }
}
