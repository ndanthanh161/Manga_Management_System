using Microsoft.AspNetCore.Authorization;
using MMS.API.Authorization.Requirements;
using System.Security.Claims;

namespace MMS.API.Authorization.Handlers
{
    /// <summary>
    /// Kiểm tra xem User đang đăng nhập có trùng khớp với Guid của tài nguyên cá nhân cần truy cập hay không.
    /// </summary>
    public class SelfOwnershipHandler : AuthorizationHandler<SelfOwnershipRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SelfOwnershipRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Task.CompletedTask;

            var userGuid = Guid.Parse(userId);

            if (context.Resource is not Guid targetUserId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Cho phép truy cập nếu chính chủ
            if (userGuid == targetUserId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
