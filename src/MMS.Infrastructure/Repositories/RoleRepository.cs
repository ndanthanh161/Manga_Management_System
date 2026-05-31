using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(MmsDbContext context) : base(context) { }

    public async Task<Role?> GetByNameAsync(string name)
        => await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
}
