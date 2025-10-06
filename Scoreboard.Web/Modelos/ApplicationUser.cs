using Microsoft.AspNetCore.Identity;

namespace Scoreboard.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
    }
}
