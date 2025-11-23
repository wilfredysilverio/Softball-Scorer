using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Scoreboard.Web.Infra
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("IdentitySeeder");

            string[] roles = ["Admin", "Anotador", "Viewer"];
            foreach (var r in roles)
            {
                if (await roleMgr.RoleExistsAsync(r))
                {
                    logger?.LogInformation("Rol {Rol} ya existe", r);
                }
                else
                {
                    var create = await roleMgr.CreateAsync(new IdentityRole(r));
                    if (!create.Succeeded)
                    {
                        logger?.LogError("Error creando rol {Rol}: {Errores}", r, string.Join(",", create.Errors.Select(e => e.Code)));
                        throw new Exception($"No se pudo crear el rol {r}");
                    }
                    logger?.LogInformation("Rol {Rol} creado", r);
                }
            }

            // Usuario admin principal
            var email = "admin@local";
            var admin = await userMgr.FindByEmailAsync(email);
            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var createUser = await userMgr.CreateAsync(admin, "Admin!2025");
                if (!createUser.Succeeded)
                {
                    logger?.LogError("No se pudo crear usuario admin: {Errores}", string.Join(",", createUser.Errors.Select(e => e.Description)));
                    throw new Exception("No se pudo crear el usuario admin principal");
                }
                logger?.LogInformation("Usuario admin creado {Email}", email);
            }
            else
            {
                logger?.LogInformation("Usuario admin ya existe {Email}", email);
            }

            if (!await userMgr.IsInRoleAsync(admin, "Admin"))
            {
                var addRole = await userMgr.AddToRoleAsync(admin, "Admin");
                if (!addRole.Succeeded)
                {
                    logger?.LogError("No se pudo asignar rol Admin al usuario {Email}", email);
                    throw new Exception("No se pudo asignar rol Admin al usuario admin");
                }
                logger?.LogInformation("Rol Admin asignado a {Email}", email);
            }
            else
            {
                logger?.LogInformation("Usuario {Email} ya tiene rol Admin", email);
            }
        }
    }
}
