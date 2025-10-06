using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.Web.Models;
using Scoreboard.Web.ViewModels;
using System.Threading.Tasks;

namespace Scoreboard.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        // Evita ReturnUrl hacia /Account/... para no crear loops
        private string CleanReturn(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)) return "/";
            // Aceptamos solo URLs locales y que NO comiencen con /Account
            if (Url.IsLocalUrl(returnUrl) && !returnUrl.StartsWith("/Account", System.StringComparison.OrdinalIgnoreCase))
                return returnUrl;
            return "/";
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Register(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = CleanReturn(returnUrl);
            return View(new RegisterViewModel());
        }

        [HttpPost, AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel m, string returnUrl = "/")
        {
            returnUrl = CleanReturn(returnUrl);
            if (!ModelState.IsValid) return View(m);

            var user = new ApplicationUser { UserName = m.Email, Email = m.Email, PhoneNumber = m.Phone, FullName = m.FullName };
            var result = await _users.CreateAsync(user, m.Password);

            if (result.Succeeded)
            {
                await _signIn.SignInAsync(user, isPersistent: true);
                return LocalRedirect(returnUrl);
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            return View(m);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = CleanReturn(returnUrl);
            return View(new LoginViewModel());
        }

        [HttpPost, AllowAnonymous]
        [ValidateAntiForgeryToken]




        public async Task<IActionResult> Login(LoginViewModel m, string returnUrl = "/")
        {
            if (!ModelState.IsValid) return View(m);

            // (Opcional) buscar usuario por correo para diferenciar mensajes
            var user = await _users.FindByEmailAsync(m.Email);

            var result = await _signIn.PasswordSignInAsync(m.Email, m.Password, m.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return LocalRedirect(returnUrl);

            // Mensaje único en rojo
            ModelState.AddModelError(string.Empty,
                "Error: correo o contraseña incorrectos. Si no recuerdas tus datos, crea una cuenta nueva.");

          

            return View(m);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            // Tras salir, llévalo al login
            return RedirectToAction(nameof(Login), "Account");
        }
    }
}
