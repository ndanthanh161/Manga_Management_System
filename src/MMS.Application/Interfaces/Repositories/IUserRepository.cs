using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetUserWithRolesAsync(Guid userId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User> ProcessInviteRegistrationAsync(string email, string username, string passwordHash, string roleName, Guid? invitedById);
    Task UpdateLoginSessionAsync(Guid userId, string refreshToken);
    Task RemoveRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<User>> GetAssistantsByMangakaAsync(Guid mangakaId);
    Task<IEnumerable<User>> GetAllWithRolesAsync();
}
