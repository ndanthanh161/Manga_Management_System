using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Repositories;
using MMS.Infrastructure.Data;
using MMS.Infrastructure.Repositories;

namespace MMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MmsDbContext _context;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IInviteRepository? _invites;

    public UnitOfWork(MmsDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IInviteRepository Invites => _invites ??= new InviteRepository(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
