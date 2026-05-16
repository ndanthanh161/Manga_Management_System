using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IInviteRepository : IGenericRepository<UserInvite>
{
    Task<UserInvite?> GetByTokenAsync(string token);
    Task<UserInvite> CreateInvitationAsync(string email, string roleName, Guid invitedByUserId);
    Task MarkAsUsedAsync(string token);
}
