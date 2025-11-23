using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace Scoreboard.Web.Infra.Email
{
    public class DummyEmailSender : IEmailSender
    {
        private readonly ILogger<DummyEmailSender> _logger;
        private readonly IConfiguration _config;
        public DummyEmailSender(ILogger<DummyEmailSender> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mode = _config["Email:Mode"] ?? "Log";
            if (!string.Equals(mode, "Log", StringComparison.OrdinalIgnoreCase))
            {
                // Future: implement SMTP/Provider when not in Log mode
            }
            _logger.LogInformation("[EMAIL-LOG] To: {Email} | Subject: {Subject} | Body: {Body}", email, subject, htmlMessage);
            Console.WriteLine($"[EMAIL-LOG] To: {email} | Subject: {subject} | Body: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}
