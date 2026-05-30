using HotChocolate.Execution.Processing;
using Microsoft.AspNetCore.Authorization;
using MMS.API.Authorization.Requirements;
using MMS.Domain.Entities;
using System.Security.Claims;

namespace MMS.API.Authorization.Handlers
{
    public class SeriesOwnershipHandler : AuthorizationHandler<SeriesOwnershipRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SeriesOwnershipRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null) return Task.CompletedTask;
            var userGuid = Guid.Parse(userId);

            if (context.Resource is not Series series)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (series.AuthorId == userGuid)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if(series.TantouEditorId == userGuid)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if(context.User.IsInRole("Editorial Board"))
            {
                context.Succeed(requirement);       
            }
            return Task.CompletedTask;
        }
    }
}
