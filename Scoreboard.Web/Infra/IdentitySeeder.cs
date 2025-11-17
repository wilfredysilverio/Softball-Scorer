using Microsoft.AspNetCore.Identity;
using Scoreboard.Web.Models;

namespace Scoreboard.Web.Infra
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var roleName = "Admin";
            if (!await roleMgr.RoleExistsAsync(roleName))
                await roleMgr.CreateAsync(new IdentityRole(roleName));

            // Usuario demo solo en desarrollo
            var email = "admin@demo.com";
            var user = await userMgr.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = "Administrador del sistema"
                };
                var create = await userMgr.CreateAsync(user, "Softball!2025");
                if (!create.Succeeded) throw new Exception("No se pudo crear el usuario admin");
            }

            if (!await userMgr.IsInRoleAsync(user, roleName))
                await userMgr.AddToRoleAsync(user, roleName);
        }
    }
}
