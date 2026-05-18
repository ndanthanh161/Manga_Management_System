namespace MMS.Application.DTOs.User;

public record UserDto(Guid Id, string Username, string Email, bool IsActive, List<string> Roles, Guid? MangakaId);
