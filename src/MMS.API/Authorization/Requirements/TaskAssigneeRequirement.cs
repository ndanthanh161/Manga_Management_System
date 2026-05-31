using Microsoft.AspNetCore.Authorization;

namespace MMS.API.Authorization.Requirements
{
    /// <summary>
    /// Yêu cầu: User đang đăng nhập phải đúng là người được giao (Assignee) cho AssistantTask.
    /// </summary>
    public class TaskAssigneeRequirement : IAuthorizationRequirement { }
}
