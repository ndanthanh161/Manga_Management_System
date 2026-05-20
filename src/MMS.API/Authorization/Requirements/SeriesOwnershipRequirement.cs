using Microsoft.AspNetCore.Authorization;

namespace MMS.API.Authorization.Requirements
{

    /// <summary>
    /// Yêu cầu: User phải là Author/Owner của Series.
    /// </summary>
    public class SeriesOwnershipRequirement : IAuthorizationRequirement { }
}
