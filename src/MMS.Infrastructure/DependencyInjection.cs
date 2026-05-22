using CloudinaryDotNet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Repositories;
using MMS.Application.Interfaces.Services;
using MMS.Infrastructure.Data;
using MMS.Infrastructure.gRPC;
using MMS.Infrastructure.Repositories;
using MMS.Infrastructure.Services;
using MMS.Infrastructure.Services.Storage;

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

        // 5. Image Storage (Cloudinary)
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.AddSingleton(sp => {
            var opts = sp.GetRequiredService<IOptions<CloudinaryOptions>>().Value;
            return new Cloudinary(new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret));
        });
        services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
        services.AddSingleton<IFileValidator, FileValidator>();

        return services;
    }
}
