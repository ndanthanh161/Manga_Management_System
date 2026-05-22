using MMS.Application.DTOs.Auth;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using R = MMS.Application.Common;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class AuthMutations
{
    public async Task<R.Result<AuthResponse>> Login(
        [Service] IAuthService authService,
        LoginRequest request)
    {
        return await authService.LoginAsync(request);
    }

    public async Task<R.Result<UserInviteDto>> InviteUser(
        [Service] IAuthService authService,
        InviteRequest request,
        ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return R.Result<UserInviteDto>.Failure("UNAUTHORIZED", "Unauthorized", 401);

        return await authService.InviteUserAsync(request, Guid.Parse(userIdString));
    }

    public async Task<R.Result<AuthResponse>> Register(
        [Service] IAuthService authService,
        RegisterRequest request)
    {
        return await authService.RegisterByInviteAsync(request);
    }

    public async Task<R.Result<AuthResponse>> RefreshToken(
        [Service] IAuthService authService,
        string refreshToken)
    {
        return await authService.RefreshTokenAsync(refreshToken);
    }

    [Authorize]
    public async Task<R.Result<bool>> Logout(
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor httpContextAccessor,
        string? refreshToken)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        var accessToken = authHeader?.Replace("Bearer ", "") ?? "";
        
        return await authService.LogoutAsync(accessToken, refreshToken ?? "");
    }
}
