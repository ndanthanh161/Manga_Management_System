using Mapster;
using MMS.Application.Common;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;

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
        var user = await _unitOfWork.Users.GetByEmailAsync(request.email);

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
        var invitingUser = await _unitOfWork.Users.GetUserWithRolesAsync(currentUserId);
        if (invitingUser == null)
        {
            return Result<UserInviteDto>.Failure("USER_NOT_FOUND", "Inviting user not found", 404);
        }

        var invitingUserRoles = invitingUser.Roles.Select(r => r.Name).ToList();

        if (request.RoleName.Equals("Mangaka", StringComparison.OrdinalIgnoreCase))
        {
            if (!invitingUserRoles.Contains("Admin") &&
                !invitingUserRoles.Contains("Editor") &&
                !invitingUserRoles.Contains("Editorial Board"))
            {
                return Result<UserInviteDto>.Failure("FORBIDDEN", "Bạn không có quyền mời Mangaka. Chỉ Admin hoặc Editor mới có quyền này.", 403);
            }
        }
        else if (request.RoleName.Equals("Assistant", StringComparison.OrdinalIgnoreCase))
        {
            if (!invitingUserRoles.Contains("Mangaka"))
            {
                return Result<UserInviteDto>.Failure("FORBIDDEN", "Bạn không có quyền mời Trợ lý. Chỉ Mangaka mới có quyền này.", 403);
            }
        }
        else
        {
            return Result<UserInviteDto>.Failure("INVALID_ROLE", $"Vai trò '{request.RoleName}' không được hỗ trợ mời qua email.", 400);
        }

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

        var existingUser = await _unitOfWork.Users.GetByEmailAsync(invite.Email);
        User user;
        if (existingUser != null)
        {
            user = existingUser;
            user.PasswordHash = passwordHash;
            _unitOfWork.Users.Update(user);

            var rolesList = (await _unitOfWork.Roles.FindAsync(r => r.Name == invite.RoleName)).ToList();
            if (rolesList.Any())
            {
                var roleToAdd = rolesList.First();
                var userWithRoles = await _unitOfWork.Users.GetUserWithRolesAsync(user.Id);
                if (userWithRoles != null && !userWithRoles.Roles.Any(r => r.Name == invite.RoleName))
                {
                    userWithRoles.Roles.Add(roleToAdd);
                    _unitOfWork.Users.Update(userWithRoles);
                }
            }
        }
        else
        {
            user = await _unitOfWork.Users.ProcessInviteRegistrationAsync(
                invite.Email,
                request.Username,
                passwordHash,
                invite.RoleName,
                invite.InvitedById);
        }

        // Đồng bộ hóa tác phẩm (nếu là Mangaka)
        if (invite.RoleName.Equals("Mangaka", StringComparison.OrdinalIgnoreCase))
        {
            var unassociatedSeries = await _unitOfWork.Series.FindAsync(s => s.AuthorEmail == invite.Email);
            foreach (var series in unassociatedSeries)
            {
                series.AuthorId = user.Id;
                _unitOfWork.Series.Update(series);
            }
        }

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
