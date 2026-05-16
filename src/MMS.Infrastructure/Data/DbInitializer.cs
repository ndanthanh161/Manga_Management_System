using Microsoft.Extensions.DependencyInjection;
using MMS.Domain.Entities;
using MMS.Application.Interfaces;
using MMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MMS.Infrastructure.Data;

public static class DbInitializer
{
    public static async System.Threading.Tasks.Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MmsDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        context.Database.EnsureCreated();

        if (await context.Roles.AnyAsync()) return;

        // Định nghĩa các Role theo yêu cầu
        var adminRole = new Role { Name = "Admin" };
        var mangakaRole = new Role { Name = "Mangaka" };
        var assistantRole = new Role { Name = "Assistant" };
        var tantouEditorRole = new Role { Name = "Tantou Editor" };
        var editorialBoardRole = new Role { Name = "Editorial Board" };

        await context.Roles.AddRangeAsync(adminRole, mangakaRole, assistantRole, tantouEditorRole, editorialBoardRole);

        // Seed Admin User mặc định
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@mms.com",
            PasswordHash = passwordHasher.Hash("Admin@123"),
            Roles = new List<Role> { adminRole }
        };
        
        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }
}
