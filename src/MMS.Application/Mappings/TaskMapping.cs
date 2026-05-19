using Mapster;
using MMS.Domain.Entities;
using MMS.Application.DTOs.Task;
using MMS.Application.DTOs.Region;

namespace MMS.Application.Mappings;

public class TaskMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // WorkTask -> TaskResponse
        config.NewConfig<WorkTask, TaskResponse>()
            .Map(dest => dest.AssignedUserName, src => src.AssignedUser != null ? src.AssignedUser.Username : null)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Region, src => src.Region)
            .Map(dest => dest.PageImageUrl, src => src.Region != null && src.Region.Page != null ? src.Region.Page.ImageUrl : null);

        // AssignTaskRequest -> WorkTask
        config.NewConfig<AssignTaskRequest, WorkTask>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AssignedUser!)
            .Ignore(dest => dest.Region!)
            .Map(dest => dest.AssignedUserId, src => src.AssistantId)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow);
    }
}
