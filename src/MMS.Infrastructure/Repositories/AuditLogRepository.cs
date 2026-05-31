using Microsoft.EntityFrameworkCore;
using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly MmsDbContext _context;

    public AuditLogRepository(MmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> QueryAsync(
        string? entityName = null, Guid? entityId = null,
        Guid? actorUserId = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 20)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        if (actorUserId.HasValue)
            query = query.Where(a => a.ActorUserId == actorUserId.Value);

        if (from.HasValue)
            query = query.Where(a => a.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.OccurredAt <= to.Value);

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
