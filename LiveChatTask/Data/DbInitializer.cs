using LiveChatTask.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiveChatTask.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            
            // Ensure DB schema is up-to-date (required for settings/presence/chat tables).
            await context.Database.MigrateAsync();

            // ========================
            // 1?? Roles
            // ========================
            string[] roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ========================
            // 2?? Seed Admin
            // ========================
            string adminEmail = "admin@livechat.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    Role = "Admin",
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ========================
            // 3?? Seed Users
            // ========================
            for (int i = 1; i <= 5; i++)
            {
                string userEmail = $"user{i}@livechat.com";
                var user = await userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = $"user{i}",
                        Email = userEmail,
                        Role = "User",
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user, "User123!");
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            await context.SaveChangesAsync();

            // ========================
            // 4) Seed ChatSettings (singleton)
            // ========================
            var settings = await context.ChatSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                context.ChatSettings.Add(new ChatSettings
                {
                    MaxUserMessageLength = 500,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByAdminId = admin.Id
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
