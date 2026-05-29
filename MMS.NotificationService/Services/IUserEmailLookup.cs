namespace MMS.NotificationService.Services;

public interface IUserEmailLookup
{
    Task<string?> GetEmailAsync(string userId);
}
