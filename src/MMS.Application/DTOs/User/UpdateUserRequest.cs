namespace MMS.Application.DTOs.User;

public record UpdateUserRequest(Guid Id, string? Username, string? Email, bool? IsActive, List<string>? Roles);
