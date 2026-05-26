namespace MMS.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendInvitationAsync(string email, string token, string roleName);
    Task SendUnderperformanceAlertAsync(string email, string authorName, string seriesTitle, int currentRanking);
    Task SendApplicationApprovedAsync(string email, string fullName, string seriesTitle);
    Task SendApplicationRejectedAsync(string email, string fullName, string seriesTitle, string? reason);
}
