using Microsoft.AspNetCore.Authorization;

namespace MMS.API.Authorization.Requirements
{
    /// <summary>
    /// Yêu cầu: ID của dữ liệu cần truy cập phải thuộc sở hữu của chính User đang đăng nhập.
    /// </summary>
    public class SelfOwnershipRequirement : IAuthorizationRequirement { }
}
