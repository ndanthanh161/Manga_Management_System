using System;

namespace MMS.Domain.Entities;

public class AssistantIncome
{
    public Guid Id { get; set; }
    
    public Guid AssistantId { get; set; }
    public User Assistant { get; set; } = null!;
    
    public int Year { get; set; }
    public int Month { get; set; }
    
    public decimal BaseSalary { get; set; }
    public int OnTimeTasksCount { get; set; }
    public int TotalTasksCount { get; set; }
    public double PerformanceRate { get; set; }
    public decimal PerformanceBonus { get; set; }
    public decimal TotalIncome { get; set; }
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
