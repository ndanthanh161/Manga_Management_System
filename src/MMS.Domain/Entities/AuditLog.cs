namespace MMS.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityDisplayName { get; set; }
    public Guid EntityId { get; set; }
    public MMS.Domain.Enums.AuditAction Action { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? Source { get; set; }
}
