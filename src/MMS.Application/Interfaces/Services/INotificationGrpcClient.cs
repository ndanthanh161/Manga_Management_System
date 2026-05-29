namespace MMS.Application.Interfaces.Services;

public interface INotificationGrpcClient
{
    Task SendEmailAsync(string userId, string title, string body);
    Task FanOutEmailAsync(IEnumerable<string> userIds, string title, string body);
}
