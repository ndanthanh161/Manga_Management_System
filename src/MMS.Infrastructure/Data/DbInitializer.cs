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

        context.Database.Migrate();

        // Seed Roles nếu chưa có
        var roleNames = new[] { "Mangaka", "Assistant", "Tantou Editor", "Editorial Board" };

        var existingRoles = await context.Roles.Where(r => roleNames.Contains(r.Name)).Select(r => r.Name).ToListAsync();

        var missingRoles = roleNames.Where(name => !existingRoles.Contains(name)).Select(name => new Role { Name = name }).ToList();

        if (missingRoles.Any())
        {
            await context.Roles.AddRangeAsync(missingRoles);
            await context.SaveChangesAsync();
        }

        // Seed Editorial Board member nếu chưa tồn tại
        if (!await context.Users.AnyAsync(u => u.Email == "eb_member1@mms.com"))
        {
            var ebRole = await context.Roles.FirstAsync(r => r.Name == "Editorial Board");

            var ebMember = new User
            {
                Id = Guid.NewGuid(),
                Username = "eb_member1",
                Email = "eb_member1@mms.com",
                PasswordHash = passwordHasher.Hash("Eb@123456"),
                IsActive = true,
                Roles = new List<Role> { ebRole }
            };

            await context.Users.AddAsync(ebMember);
            await context.SaveChangesAsync();
        }
    }
}
