using Microsoft.EntityFrameworkCore;
using MMS.Infrastructure.Data;

namespace MMS.NotificationService.Services;

public class DbUserEmailLookup : IUserEmailLookup
{
    private readonly MmsDbContext _db;

    public DbUserEmailLookup(MmsDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetEmailAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid)) 
            return null;

        var email = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == guid)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        return email;
    }
}
