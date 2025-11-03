using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.Web.Modelos;          // ✅ ApplicationUser vive aquí
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
            if (Url.IsLocalUrl(returnUrl) &&
                !returnUrl.StartsWith("/Account", System.StringComparison.OrdinalIgnoreCase))
                return returnUrl;
            return "/";
        }

        // ===== REGISTER =====
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

            var user = new ApplicationUser
            {
                UserName = m.Email,                // si usas email como username
                Email = m.Email,
                PhoneNumber = m.Phone,
                FullName = m.FullName
            };

            var result = await _users.CreateAsync(user, m.Password);
            if (result.Succeeded)
            {
                await _signIn.SignInAsync(user, isPersistent: true);
                return LocalRedirect(returnUrl);
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return View(m);
        }

        // ===== LOGIN =====
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
            returnUrl = CleanReturn(returnUrl);
            if (!ModelState.IsValid) return View(m);

            // Buscar por email y firmar con el UserName real (por si no coincide con el email)
            var user = await _users.FindByEmailAsync(m.Email);
            if (user != null)
            {
                var result = await _signIn.PasswordSignInAsync(user.UserName, m.Password, m.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                    return LocalRedirect(returnUrl);
            }
            else
            {
                // fallback: intentar con el email tal cual (si tu UserName es el email)
                var result = await _signIn.PasswordSignInAsync(m.Email, m.Password, m.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                    return LocalRedirect(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Error: correo o contraseña incorrectos.");
            return View(m);
        }

        // ===== LOGOUT =====
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
