using MMS.Application.DTOs.Task;
using MMS.Domain.Enums;

namespace MMS.Application.Interfaces.Services;

public interface ITaskService
{
    Task<TaskResponse> AssignTaskAsync(Guid mangakaId, AssignTaskRequest request);
    Task<IEnumerable<TaskResponse>> GetByAssignedUserAsync(Guid userId);
    Task<TaskResponse> UpdateStatusAsync(Guid taskId, Guid requesterId, UpdateTaskStatusRequest request);
    Task<IEnumerable<MonthlyIncomeStats>> GetMonthlyIncomeStatsAsync(Guid assistantId);
    Task<IEnumerable<TaskResponse>> GetByPageAsync(Guid pageId);
    Task<IEnumerable<TaskResponse>> GetByMangakaAsync(Guid mangakaId);
}
