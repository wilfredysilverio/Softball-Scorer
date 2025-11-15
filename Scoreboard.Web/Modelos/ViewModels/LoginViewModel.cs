namespace Scoreboard.Web.Modelos.ViewModels
{
    public class LoginViewModel
    {
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }
}
