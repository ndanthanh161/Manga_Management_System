using MMS.Application.Interfaces.Repositories;

namespace MMS.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IInviteRepository Invites { get; }
    
    Task<int> SaveChangesAsync();
}
