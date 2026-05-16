using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MMS.Application.Interfaces.Services;
using MMS.Application.Services;

namespace MMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>(); // Added back
        
        return services;
    }
}
