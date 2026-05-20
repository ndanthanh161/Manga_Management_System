using System;

namespace MMS.Domain.Entities;

public class AssistantSalary
{
    public Guid Id { get; set; }
    
    public Guid AssistantId { get; set; }
    public User Assistant { get; set; } = null!;
    
    public decimal BaseSalary { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
