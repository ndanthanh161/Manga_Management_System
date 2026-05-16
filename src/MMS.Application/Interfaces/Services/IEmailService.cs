namespace MMS.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendInvitationAsync(string email, string token, string roleName);
}
