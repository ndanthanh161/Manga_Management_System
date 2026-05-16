using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class InviteRepository : GenericRepository<UserInvite>, IInviteRepository
{
    public InviteRepository(MmsDbContext context) : base(context) { }

    public async Task<UserInvite?> GetByTokenAsync(string token)
        => await _context.UserInvites.FirstOrDefaultAsync(i => i.Token == token);

    public async Task<UserInvite> CreateInvitationAsync(string email, string roleName, Guid invitedByUserId)
    {
        var invite = new UserInvite
        {
            Email = email,
            Token = Guid.NewGuid().ToString("N"),
            RoleName = roleName,
            ExpiryDate = DateTime.UtcNow.AddDays(3),
            InvitedById = invitedByUserId // Fixed property name
        };
        await _context.UserInvites.AddAsync(invite);
        return invite;
    }

    public async Task MarkAsUsedAsync(string token)
    {
        var invite = await GetByTokenAsync(token);
        if (invite != null)
        {
            invite.IsUsed = true;
            invite.UsedAt = DateTime.UtcNow; // Now exists
        }
    }
}
