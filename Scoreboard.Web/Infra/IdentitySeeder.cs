using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Scoreboard.Web.Modelos.Identity;

namespace Scoreboard.Web.Infra
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacion>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<RolAplicacion>>();

            var role = "Admin";
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new RolAplicacion { Name = role });

            var userName = "admin";
            var email = "admin@local.test";
            var pass = "Admin@123";

            var u = await users.FindByNameAsync(userName);
            if (u == null)
            {
                u = new UsuarioAplicacion { UserName = userName, Email = email, EmailConfirmed = true };
                var r = await users.CreateAsync(u, pass);
                if (r.Succeeded)
                    await users.AddToRoleAsync(u, role);
            }
        }
    }
}
