using Mapster;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class TaskMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // WorkTask -> TaskResponse
        config.NewConfig<WorkTask, TaskResponse>()
            .Map(dest => dest.AssignedUserName, src => src.AssignedUser != null ? src.AssignedUser.Username : null)
            .Map(dest => dest.Status, src => src.Status.ToString());

        // AssignTaskRequest -> WorkTask
        config.NewConfig<AssignTaskRequest, WorkTask>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AssignedUser!)
            .Ignore(dest => dest.Region!)
            .Map(dest => dest.AssignedUserId, src => src.AssistantId)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow);
    }
}
