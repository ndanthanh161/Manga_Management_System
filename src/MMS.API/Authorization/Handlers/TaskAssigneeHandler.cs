using Microsoft.AspNetCore.Authorization;
using MMS.API.Authorization.Requirements;
using MMS.Domain.Entities;
using System.Security.Claims;

namespace MMS.API.Authorization.Handlers
{
    public class TaskAssigneeHandler : AuthorizationHandler<TaskAssigneeRequirement>
    {
        protected override Task HandleRequirementAsync(
             AuthorizationHandlerContext context,
             TaskAssigneeRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Task.CompletedTask;

            var userGuid = Guid.Parse(userId);

            if (context.Resource is not WorkTask task)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Kiểm tra xem Trợ lý đăng nhập có đúng là người được giao task hay không
            if (task.AssignedUserId == userGuid)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
