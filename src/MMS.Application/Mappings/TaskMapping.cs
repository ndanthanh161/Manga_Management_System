using Mapster;
using TaskEntity = MMS.Domain.Entities.Task;

namespace MMS.Application.Mappings;

public class TaskMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Task -> TaskResponse
        config.NewConfig<TaskEntity, TaskResponse>()
            .Map(dest => dest.AssignedUserName, src => src.AssignedUser != null ? src.AssignedUser.Username : null)
            .Map(dest => dest.Status, src => src.Status.ToString());

        // AssignTaskRequest -> Task
        config.NewConfig<AssignTaskRequest, TaskEntity>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AssignedUser!)
            .Ignore(dest => dest.Region!)
            .Map(dest => dest.AssignedUserId, src => src.AssistantId)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow);
    }
}
