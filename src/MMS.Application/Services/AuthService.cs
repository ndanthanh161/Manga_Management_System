using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Application.Common;
using MMS.Domain.Entities;
using Mapster;

namespace MMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;

    public AuthService(
        IUnitOfWork unitOfWork, 
        IPasswordHasher passwordHasher, 
        IJwtProvider jwtProvider, 
        ICacheService cacheService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _cacheService = cacheService;
        _emailService = emailService;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.EmailOrUsername) 
                   ?? await _unitOfWork.Users.GetByUsernameAsync(request.EmailOrUsername);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("INVALID_CREDENTIALS", "Invalid credentials", 401);

        if (!user.IsActive)
            return Result<AuthResponse>.Failure("ACCOUNT_DEACTIVATED", "Account is deactivated", 403);

        var accessToken = _jwtProvider.GenerateAccessToken(user);
        var refreshToken = _jwtProvider.GenerateRefreshToken();

        await _unitOfWork.Users.UpdateLoginSessionAsync(user.Id, refreshToken);
        await _unitOfWork.SaveChangesAsync();

        var response = user.Adapt<AuthResponse>();
        response.AccessToken = accessToken;
        response.RefreshToken = refreshToken;
        // Roles được Mapster tự map qua AuthMapping

        return Result<AuthResponse>.Success(response, "Login successful");
    }

    public async Task<Result<UserInviteDto>> InviteUserAsync(InviteRequest request, Guid currentUserId)
    {
        var invite = await _unitOfWork.Invites.CreateInvitationAsync(
            request.Email, 
            request.RoleName, 
            currentUserId);

        await _unitOfWork.SaveChangesAsync();
        await _emailService.SendInvitationAsync(request.Email, invite.Token, request.RoleName);

        return Result<UserInviteDto>.Success(
            new UserInviteDto(invite.Email, invite.Token, invite.RoleName, invite.ExpiryDate),
            "Invitation sent successfully");
    }

    public async Task<Result<AuthResponse>> RegisterByInviteAsync(RegisterRequest request)
    {
        var invite = await _unitOfWork.Invites.GetByTokenAsync(request.Token);
        if (invite == null || invite.IsUsed || invite.ExpiryDate < DateTime.UtcNow)
            return Result<AuthResponse>.Failure("INVALID_TOKEN", "Invalid or expired invitation token");

        var passwordHash = _passwordHasher.Hash(request.Password);
        
        var user = await _unitOfWork.Users.ProcessInviteRegistrationAsync(
            invite.Email, 
            request.Username, 
            passwordHash, 
            invite.RoleName,
            invite.InvitedById);

        await _unitOfWork.Invites.MarkAsUsedAsync(invite.Token);
        await _unitOfWork.SaveChangesAsync();

        // Tự động đăng nhập sau khi đăng ký
        var loginResult = await LoginAsync(new LoginRequest(invite.Email, request.Password));
        if (loginResult.Succeeded)
        {
            loginResult.Message = "Registration successful and logged in";
        }
        return loginResult;
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var user = await _unitOfWork.Users.GetByRefreshTokenAsync(refreshToken);
        if (user == null) return Result<AuthResponse>.Failure("INVALID_TOKEN", "Invalid refresh token");

        var tokenModel = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);
        if (tokenModel == null || tokenModel.IsExpired)
            return Result<AuthResponse>.Failure("TOKEN_EXPIRED", "Token expired or invalid");

        var newAccessToken = _jwtProvider.GenerateAccessToken(user);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();

        await _unitOfWork.Users.RemoveRefreshTokenAsync(refreshToken);
        await _unitOfWork.Users.UpdateLoginSessionAsync(user.Id, newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        var response = user.Adapt<AuthResponse>();
        response.AccessToken = newAccessToken;
        response.RefreshToken = newRefreshToken;
        // Roles được Mapster tự map qua AuthMapping

        return Result<AuthResponse>.Success(response, "Token refreshed successfully");
    }

    public async Task<Result<bool>> LogoutAsync(string accessToken, string refreshToken)
    {
        if (!string.IsNullOrEmpty(accessToken))
            await _cacheService.SetAsync($"blacklist_{accessToken}", true, TimeSpan.FromHours(1));

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _unitOfWork.Users.RemoveRefreshTokenAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
        }

        return Result<bool>.Success(true, "Logged out successfully");
    }
}
