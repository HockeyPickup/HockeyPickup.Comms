using Microsoft.Extensions.Logging;

namespace HockeyPickup.Comms.Services;

public interface ICommsHandler
{
    Task SendSignedInEmail(string email);
    Task SendRegistrationConfirmationEmail(string Email, string UserId, string FirstName, string LastName, string ConfirmationUrl);
    Task SendForgotPasswordEmail(string Email, string UserId, string FirstName, string LastName, string ResetUrl);
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

    public async Task SendForgotPasswordEmail(string Email, string UserId, string FirstName, string LastName, string ResetUrl)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Forgot Password email for: {Email}");

            if (string.IsNullOrEmpty(Email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(Email));
            }

            await _emailService.SendEmailAsync(Email, "Reset Password Request", EmailTemplate.ForgotPassword,
                new Dictionary<string, string> { { "EMAIL", Email }, { "FIRSTNAME", FirstName }, { "LASTNAME", LastName }, { "RESET_URL", ResetUrl } });

            _logger.LogInformation($"CommsHandler->Successfully sent Forgot Password email for: {Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Forgot Password email for: {Email}");

            throw;
        }
    }

    public async Task SendRegistrationConfirmationEmail(string Email, string UserId, string FirstName, string LastName, string ConfirmationUrl)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Registration Confirmation email for: {Email}");

            if (string.IsNullOrEmpty(Email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(Email));
            }

            await _emailService.SendEmailAsync(Email, "Registration Confirmation", EmailTemplate.Register,
                new Dictionary<string, string> { { "EMAIL", Email }, { "FIRSTNAME", FirstName }, { "LASTNAME", LastName }, { "CONFIRMATION_URL", ConfirmationUrl } });

            _logger.LogInformation($"CommsHandler->Successfully sent Registration Confirmation email for: {Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Registration Confirmation email for: {Email}");

            throw;
        }
    }

    public async Task SendSignedInEmail(string email)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Signed In email for: {email}");

            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            await _emailService.SendEmailAsync(email, "Signed In Alert", EmailTemplate.SignedIn,
                new Dictionary<string, string> { { "EMAIL", email } });

            _logger.LogInformation($"CommsHandler->Successfully sent Signed In email for: {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Signed In email for: {email}");

            throw;
        }
    }
}
