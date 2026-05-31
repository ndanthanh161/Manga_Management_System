using Microsoft.AspNetCore.Authorization;

namespace MMS.API.Authorization.Requirements
{
    /// <summary>
    /// Yêu cầu: User đang đăng nhập phải là Mangaka sở hữu series chứa AssistantTask này.
    /// </summary>
    public class TaskOwnerRequirement : IAuthorizationRequirement { }
}
