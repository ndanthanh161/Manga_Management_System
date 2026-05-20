using Microsoft.AspNetCore.Authorization;

namespace MMS.API.Authorization.Requirements
{
    /// <summary>
    /// Yêu cầu: User phải là Tantou được assign cho resource (Series/Submission).
    /// </summary>
    public class TantouAssignmentRequirement : IAuthorizationRequirement { }
}
