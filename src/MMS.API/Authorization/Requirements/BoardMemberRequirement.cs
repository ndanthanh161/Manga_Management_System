using Microsoft.AspNetCore.Authorization;

namespace MMS.API.Authorization.Requirements
{
    /// <summary>
    /// Yêu cầu: User phải có role EditorialBoard.
    /// </summary>
    public class BoardMemberRequirement : IAuthorizationRequirement { }
}
