namespace MMS.Application.DTOs;

public record UserDto(Guid Id, string Username, string Email, bool IsActive, List<string> Roles);
public record CreateUserRequest(string Username, string Email, string Password, List<string> Roles);
public record UpdateUserRequest(Guid Id, string? Username, string? Email, bool? IsActive, List<string>? Roles);
