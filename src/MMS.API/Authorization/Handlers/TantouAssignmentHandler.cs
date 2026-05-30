using Microsoft.AspNetCore.Authorization;
using MMS.API.Authorization.Requirements;
using MMS.Domain.Entities;
using System.Security.Claims;

namespace MMS.API.Authorization.Handlers
{
    public class TantouAssignmentHandler
    : AuthorizationHandler<TantouAssignmentRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TantouAssignmentRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Task.CompletedTask;
            var userGuid = Guid.Parse(userId);

            if (context.Resource is not Series series)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Tantou được assign cho series này?
            if (series.TantouEditorId == userGuid)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            // Board member được xem tất cả
            if (context.User.IsInRole("Editorial Board"))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
