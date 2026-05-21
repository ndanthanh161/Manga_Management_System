using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Repositories;
using MMS.Application.Interfaces.Services;
using MMS.Infrastructure.Data;
using MMS.Infrastructure.gRPC;
using MMS.Infrastructure.Repositories;
using MMS.Infrastructure.Services;

namespace MMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database & Cache
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<MmsDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetSection("Redis:ConnectionString").Value;
            options.InstanceName = "MMS_";
        });

        // 2. Repositories & Unit of Work
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 3. Infrastructure Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IEmailService, EmailService>();

        // 4. gRPC Clients (Phase 2+)
        services.AddMmsGrpcClients(configuration);

        return services;
    }
}
