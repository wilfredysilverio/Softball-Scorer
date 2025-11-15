using Microsoft.AspNetCore.Identity;

namespace Scoreboard.Web.Infra
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var roleName = "Admin";
            if (!await roleMgr.RoleExistsAsync(roleName))
                await roleMgr.CreateAsync(new IdentityRole(roleName));

            // Usuario demo solo en desarrollo
            var email = "admin@demo.com";
            var user = await userMgr.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var create = await userMgr.CreateAsync(user, "Softball!2025");
                if (!create.Succeeded) throw new Exception("No se pudo crear el usuario admin");
            }

            if (!await userMgr.IsInRoleAsync(user, roleName))
                await userMgr.AddToRoleAsync(user, roleName);
        }
    }
}
