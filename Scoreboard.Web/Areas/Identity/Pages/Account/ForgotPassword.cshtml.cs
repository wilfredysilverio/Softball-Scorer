using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Scoreboard.Web.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // No reveles existencia
                _logger.LogInformation("Solicitud de reset para email no existente {Email}", Input.Email);
                return RedirectToPage("ForgotPasswordConfirmation");
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.PageLink(pageName: "/Account/ResetPassword", values: new { area = "Identity", email = Input.Email, token });
            await _emailSender.SendEmailAsync(Input.Email, "Reset de contraseña", $"Haz clic para restablecer: <a href='{resetUrl}'>link</a>");
            _logger.LogInformation("Reset URL generado para {Email}: {Url}", Input.Email, resetUrl);
            return RedirectToPage("ForgotPasswordConfirmation");
        }
    }
}
