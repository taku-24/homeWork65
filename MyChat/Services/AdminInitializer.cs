using Microsoft.AspNetCore.Identity;
using MyChat.Models;

namespace MyChat.Services;

public static class AdminInitializer
{
    public static async Task Seed(IServiceProvider sp)
    {
        var users = sp.GetRequiredService<UserManager<User>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var r in new[] { "admin", "user" })
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new IdentityRole<int>(r));

        var email = "admin@admin.com";
        var admin = await users.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new User
            {
                UserName = "admin",
                Email = email,
                BirthDate = DateTime.UtcNow.AddYears(-25),
                AvatarPath = "/images/avatars/default.png"
            };
            var res = await users.CreateAsync(admin, "Admin123!");
            if (res.Succeeded) await users.AddToRoleAsync(admin, "admin");
        }
    }
}