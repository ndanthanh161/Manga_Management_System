namespace MMS.Application.DTOs.User;

public record CreateUserRequest(string Username, string Email, string Password, List<string> Roles, Guid? MangakaId = null);
