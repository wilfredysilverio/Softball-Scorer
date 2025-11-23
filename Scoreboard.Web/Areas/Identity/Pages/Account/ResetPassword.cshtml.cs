using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Scoreboard.Web.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;
        public ResetPasswordModel(UserManager<IdentityUser> userManager, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Nueva contraseña")]
            public string NewPassword { get; set; } = string.Empty;
            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
            [Display(Name = "Confirmar contraseña")]
            public string ConfirmPassword { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return BadRequest();
            Input.Email = email;
            Input.Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                _logger.LogWarning("Reset para email inexistente {Email}", Input.Email);
                return RedirectToPage("ResetPasswordConfirmation");
            }
            var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reseteado para {Email}", Input.Email);
                return RedirectToPage("ResetPasswordConfirmation");
            }
            foreach (var e in result.Errors)
            {
                ModelState.AddModelError(string.Empty, e.Description);
            }
            return Page();
        }
    }
}
