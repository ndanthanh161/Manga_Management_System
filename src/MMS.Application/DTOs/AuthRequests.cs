namespace MMS.Application.DTOs;

public record LoginRequest(string EmailOrUsername, string Password);
public record InviteRequest(string Email, string RoleName);
public record RegisterRequest(string Token, string Username, string Password);
public record UserInviteDto(string Email, string Token, string RoleName, DateTime ExpiryDate);

