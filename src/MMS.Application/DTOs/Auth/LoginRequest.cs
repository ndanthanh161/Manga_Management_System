namespace MMS.Application.DTOs.Auth;

public record LoginRequest(string EmailOrUsername, string Password);
