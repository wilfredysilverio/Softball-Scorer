using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Servicios
{
    public class IdentityDataSeeder
    {
        private readonly RoleManager<RolAplicacion> _roleManager;
        private readonly UserManager<UsuarioAplicacion> _userManager;
        private readonly IConfiguration _config;

        public IdentityDataSeeder(RoleManager<RolAplicacion> roleManager, UserManager<UsuarioAplicacion> userManager, IConfiguration config)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _config = config;
        }

        public async Task SeedAsync()
        {
            var roles = new[] { "Admin", "Usuario" };
            foreach (var r in roles)
            {
                if (!await _roleManager.RoleExistsAsync(r))
                {
                    await _roleManager.CreateAsync(new RolAplicacion { Name = r });
                }
            }

            var adminUser = _config["Auth:AdminUser"] ?? "admin";
            var adminEmail = _config["Auth:AdminEmail"] ?? "admin@softball.local";
            var adminPass = _config["Auth:AdminPass"] ?? "Admin*12345";

            var user = await _userManager.FindByNameAsync(adminUser);
            if (user == null)
            {
                user = new UsuarioAplicacion { UserName = adminUser, Email = adminEmail, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, adminPass);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }
            else
            {
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}
