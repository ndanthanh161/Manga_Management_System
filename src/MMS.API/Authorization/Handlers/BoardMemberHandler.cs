using Microsoft.AspNetCore.Authorization;
using MMS.API.Authorization.Requirements;
using System.Security.Claims;

namespace MMS.API.Authorization.Handlers
{
    public class BoardMemberHandler : AuthorizationHandler<BoardMemberRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BoardMemberRequirement requirement)
        {
            if(context.User.IsInRole("Editorial Board"))
            {
                context.Succeed(requirement);               
            }
            return Task.CompletedTask;
        }
    }
}
