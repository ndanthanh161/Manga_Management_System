namespace MMS.API.Hubs;

public class NotificationHub : BaseAuthHub
{
    // Không có client→server method
    // Server push qua: Clients.Group($"user:{userId}").SendAsync("notificationReceived", data)
}
