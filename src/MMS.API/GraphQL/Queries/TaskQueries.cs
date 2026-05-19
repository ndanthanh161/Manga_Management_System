using MMS.Application.DTOs.Task;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

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

    /// <summary>Xem danh sách công việc được giao của bản thân (Trợ lý)</summary>
    [Authorize(Roles = new[] { "Assistant" })]
    public async Task<IEnumerable<TaskResponse>> GetMyTasks(
        [Service] ITaskService taskService,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await taskService.GetByAssignedUserAsync(userId);
    }

    /// <summary>Theo dõi thu nhập và số trang đã được duyệt theo từng tháng</summary>
    [Authorize(Roles = new[] { "Assistant" })]
    public async Task<IEnumerable<MonthlyIncomeStats>> GetMonthlyIncomeStats(
        [Service] ITaskService taskService,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await taskService.GetMonthlyIncomeStatsAsync(userId);
    }
}
