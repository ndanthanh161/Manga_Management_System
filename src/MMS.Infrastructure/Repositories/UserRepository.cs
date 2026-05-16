using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(MmsDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByUsernameAsync(string username)
        => await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetUserWithRolesAsync(Guid userId)
        => await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        => await _context.Users.Include(u => u.Roles).Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

    public async Task<User> ProcessInviteRegistrationAsync(string email, string username, string passwordHash, string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = passwordHash,
            Roles = role != null ? new List<Role> { role } : new List<Role>()
        };

        await _context.Users.AddAsync(user);
        return user;
    }

    public async Task UpdateLoginSessionAsync(Guid userId, string refreshToken)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            var token = new RefreshToken
            {
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserId = userId
            };
            _context.Set<RefreshToken>().Add(token);
        }
    }

    public async Task RemoveRefreshTokenAsync(string refreshToken)
    {
        var token = await _context.Set<RefreshToken>().FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null) _context.Set<RefreshToken>().Remove(token);
    }
}
