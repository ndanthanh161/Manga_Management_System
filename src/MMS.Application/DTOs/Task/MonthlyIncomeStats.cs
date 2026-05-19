namespace MMS.Application.DTOs.Task;

public record MonthlyIncomeStats(
    int Year,
    int Month,
    decimal BaseSalary,
    int OnTimeTasksCount,
    int TotalTasksCount,
    double PerformanceRate,
    decimal PerformanceBonus,
    decimal TotalIncome
);
