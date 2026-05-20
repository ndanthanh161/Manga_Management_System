using System.Security.Claims;

namespace MMS.API.GraphQL;

/// <summary>
/// Helper dùng chung cho tất cả Mutation classes để lấy UserId từ JWT.
/// </summary>
internal static class GraphQLHelper
{
    public static Guid GetCurrentUserId(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Yêu cầu đăng nhập.");
        return Guid.Parse(id);
    }

    public static Guid? GetCurrentUserIdOptional(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return null;
        return Guid.Parse(id);
    }
}
