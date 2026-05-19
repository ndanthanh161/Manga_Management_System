using Mapster;
using MMS.Application.DTOs.Task;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _uow;

    public TaskService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<TaskResponse> AssignTaskAsync(Guid mangakaId, AssignTaskRequest request)
    {
        _ = await _uow.Regions.GetByIdAsync(request.RegionId)
            ?? throw new KeyNotFoundException($"Region {request.RegionId} not found.");

        var assistant = await _uow.Users.GetUserWithRolesAsync(request.AssistantId)
            ?? throw new KeyNotFoundException($"Assistant {request.AssistantId} not found.");

        if (assistant.MangakaId != mangakaId)
        {
            throw new UnauthorizedAccessException("Bạn chỉ được phép giao việc cho Trợ lý riêng thuộc quyền quản lý của mình.");
        }

        var task = request.Adapt<WorkTask>();
        task.Id = Guid.NewGuid();
        task.Status = WorkTaskStatus.Todo;

        await _uow.Tasks.AddAsync(task);
        await _uow.SaveChangesAsync();

        // Reload với navigation properties để Adapt đúng AssignedUserName
        var created = await _uow.Tasks.GetWithRegionAsync(task.Id);
        return created!.Adapt<TaskResponse>();
    }

    public async Task<IEnumerable<TaskResponse>> GetByAssignedUserAsync(Guid userId)
    {
        var tasks = await _uow.Tasks.GetByAssignedUserIdAsync(userId);
        return tasks.Adapt<IEnumerable<TaskResponse>>();
    }

    public async Task<TaskResponse> UpdateStatusAsync(Guid taskId, Guid requesterId, UpdateTaskStatusRequest request)
    {
        var task = await _uow.Tasks.GetWithRegionAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        if (!task.AssignedUserId.HasValue)
        {
            throw new InvalidOperationException("Công việc này chưa được giao cho bất kỳ ai.");
        }

        var assistant = await _uow.Users.GetUserWithRolesAsync(task.AssignedUserId.Value)
            ?? throw new KeyNotFoundException("Trợ lý được giao việc không tồn tại.");

        bool isAssignedAssistant = task.AssignedUserId == requesterId;
        bool isManagingMangaka = assistant.MangakaId == requesterId;

        if (!isAssignedAssistant && !isManagingMangaka)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật trạng thái của công việc này.");
        }

        if (isAssignedAssistant)
        {
            // Trợ lý chỉ được cập nhật trạng thái lên InProgress hoặc gửi duyệt (Review)
            if (request.NewStatus != WorkTaskStatus.InProgress && request.NewStatus != WorkTaskStatus.Review)
            {
                throw new InvalidOperationException("Trợ lý chỉ có quyền cập nhật công việc sang 'InProgress' hoặc gửi duyệt 'Review'.");
            }

            if (request.NewStatus == WorkTaskStatus.Review)
            {
                task.ResultFileUrl = request.ResultFileUrl;
                task.ResultNote = request.ResultNote;
            }
        }
        else if (isManagingMangaka)
        {
            // Mangaka có quyền duyệt hoàn thành (Done), yêu cầu sửa đổi (NeedsRevision), hoặc hủy (Canceled)
            if (request.NewStatus != WorkTaskStatus.Done && request.NewStatus != WorkTaskStatus.NeedsRevision && request.NewStatus != WorkTaskStatus.Canceled)
            {
                throw new InvalidOperationException("Tác giả (Mangaka) chỉ có quyền cập nhật công việc sang 'Done', 'NeedsRevision' hoặc 'Canceled'.");
            }

            if (request.NewStatus == WorkTaskStatus.Done)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (request.NewStatus == WorkTaskStatus.NeedsRevision)
            {
                task.CompletedAt = null;
            }
        }

        task.Status = request.NewStatus;
        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync();

        // Reload để lấy đủ thông tin Region và Page cho mapping
        var updatedTask = await _uow.Tasks.GetWithRegionAsync(taskId);
        return updatedTask!.Adapt<TaskResponse>();
    }

    public async Task<IEnumerable<MonthlyIncomeStats>> GetMonthlyIncomeStatsAsync(Guid assistantId)
    {
        var assistant = await _uow.Users.GetByIdAsync(assistantId);
        decimal baseSalary = assistant?.BaseSalary ?? 0;

        var tasks = await _uow.Tasks.GetByAssignedUserIdAsync(assistantId);

        var stats = tasks
            .Where(t => t.Status == WorkTaskStatus.Done && t.CompletedAt.HasValue)
            .GroupBy(t => new { t.CompletedAt!.Value.Year, t.CompletedAt!.Value.Month })
            .Select(g => {
                var totalTasks = g.Count();
                var onTimeTasks = g.Count(t => !t.DueDate.HasValue || t.CompletedAt <= t.DueDate);
                var rate = totalTasks > 0 ? (double)onTimeTasks / totalTasks : 1.0;
                var bonus = baseSalary * 0.5m * (decimal)rate;
                var total = baseSalary + bonus;

                return new MonthlyIncomeStats(
                    g.Key.Year,
                    g.Key.Month,
                    baseSalary,
                    onTimeTasks,
                    totalTasks,
                    rate,
                    bonus,
                    total
                );
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToList();

        return stats;
    }
}
