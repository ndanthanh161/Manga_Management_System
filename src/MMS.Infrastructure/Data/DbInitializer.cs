using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MMS.Application.Interfaces;
using MMS.Domain.Entities;

namespace MMS.Infrastructure.Data;

public static class DbInitializer
{
    public static async System.Threading.Tasks.Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MmsDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        context.Database.EnsureCreated();

        // Seed Roles nếu chưa có
        var roleNames = new[] { "Admin", "Mangaka", "Assistant", "Tantou Editor", "Editorial Board" };

        var existingRoles = await context.Roles.Where(r => roleNames.Contains(r.Name)).Select(r => r.Name).ToListAsync();

        var missingRoles = roleNames.Where(name => !existingRoles.Contains(name)).Select(name => new Role { Name = name }).ToList();

        if (missingRoles.Any())
        {
            await context.Roles.AddRangeAsync(missingRoles);
            await context.SaveChangesAsync();
        }

        // Seed Admin User nếu chưa tồn tại
        if (!await context.Users.AnyAsync(u => u.Email == "admin@mms.com"))
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@mms.com",
                PasswordHash = passwordHasher.Hash("Admin@123"),
                IsActive = true,
                Roles = new List<Role> { adminRole }
            };

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();
        }
    }
}
