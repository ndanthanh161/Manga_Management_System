using Mapster;
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

        task.Status = request.NewStatus;
        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync();

        return task.Adapt<TaskResponse>();
    }
}
