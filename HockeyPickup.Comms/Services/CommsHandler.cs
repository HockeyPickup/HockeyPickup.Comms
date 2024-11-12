using Microsoft.Extensions.Logging;

namespace HockeyPickup.Comms.Services;

public interface ICommsHandler
{
    Task SendSignedInEmail(string email);
}

public class CommsHandler : ICommsHandler
{
    private readonly ILogger<CommsHandler> _logger;
    private readonly IEmailService _emailService;

    public CommsHandler(ILogger<CommsHandler> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task SendSignedInEmail(string email)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending signed in email for: {email}");

            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            await _emailService.SendEmailAsync(email, "Signed In Alert", EmailTemplate.SignedIn,
                new Dictionary<string, string> { { "EMAIL", email } });

            _logger.LogInformation($"CommsHandler->Successfully sent logged in email for: {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending logged in email for: {email}");

            throw;
        }
    }
}
