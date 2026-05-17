namespace MMS.Application.DTOs.Auth;

public record UserInviteDto(string Email, string Token, string RoleName, DateTime ExpiryDate);
