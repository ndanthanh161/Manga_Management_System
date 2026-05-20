using MMS.Domain.Entities;

namespace MMS.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> QueryAsync(
        string? entityName = null, Guid? entityId = null,
        Guid? actorUserId = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 20);
}
