using MMS.Application.DTOs;
using MMS.Application.Common;

namespace MMS.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<UserInviteDto>> InviteUserAsync(InviteRequest request, Guid currentUserId);
    Task<Result<AuthResponse>> RegisterByInviteAsync(RegisterRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task<Result<bool>> LogoutAsync(string accessToken, string refreshToken);
}
