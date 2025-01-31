using HockeyPickup.Api;
using Microsoft.Extensions.Logging;

namespace HockeyPickup.Comms.Services;

public interface ICommsHandler
{
    Task SendRegistrationConfirmationEmail(string Email, string UserId, string FirstName, string LastName, string ConfirmationUrl);
    Task SendForgotPasswordEmail(string Email, string UserId, string FirstName, string LastName, string ResetUrl);
    Task SendRawContentEmail(string Subject, string RawContent);
    Task SendCreateSessionEmails(ICollection<string> Emails, DateTime SessionDate, string SessionUrl, string Note, string CreatedByName);
    Task SendTeamAssignmentChangeEmail(string Email, NotificationPreference NotificationPreference, ICollection<string> Emails, DateTime SessionDate, string SessionUrl, string FirstName, string LastName, string FormerTeamAssignment, string NewTeamAssignment);
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

    public async Task SendRawContentEmail(string Subject, string RawContent)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Raw Content Email");

            var alertEmail = Environment.GetEnvironmentVariable("SignInAlertEmail");
            if (string.IsNullOrEmpty(alertEmail))
            {
                throw new ArgumentException("Alert Email cannot be null or empty", nameof(alertEmail));
            }

            await _emailService.SendEmailAsync(alertEmail, Subject, EmailTemplate.RawContent,
                new Dictionary<string, string> { { "RAWCONTENT", RawContent } });

            _logger.LogInformation($"CommsHandler->Successfully sent Raw Content email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Successfully sent Raw Content email");

            throw;
        }
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

    public async Task SendCreateSessionEmails(ICollection<string> Emails, DateTime SessionDate, string SessionUrl, string Note, string CreatedByName)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Create Session email for: {SessionDate}");

            var alertEmail = Environment.GetEnvironmentVariable("SignInAlertEmail");
            if (string.IsNullOrEmpty(alertEmail))
            {
                throw new ArgumentException("Alert Email cannot be null or empty", nameof(alertEmail));
            }

            foreach (var email in Emails)
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                await _emailService.SendEmailAsync(email, $"Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")} Created", EmailTemplate.CreateSession,
                    new Dictionary<string, string> { { "EMAIL", email }, { "SESSIONDATE", SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", SessionUrl }, { "NOTE", Note }, { "CREATEDBYNAME", CreatedByName } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {Emails.Count()} Create Session emails for: {SessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending {Emails.Count()} Create Session emails for: {SessionDate}");

            throw;
        }
    }

    public async Task SendTeamAssignmentChangeEmail(string Email, NotificationPreference NotificationPreference, ICollection<string> Emails, DateTime SessionDate, string SessionUrl, string FirstName, string LastName, string FormerTeamAssignment, string NewTeamAssignment)
    {
        try
        {
            if (NotificationPreference != NotificationPreference.None)
            {
                _logger.LogInformation($"CommsHandler->Sending Team Assignment Change email for: {Email}");

                if (string.IsNullOrEmpty(Email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(Email));
                }

                await _emailService.SendEmailAsync(Email, $"Team Assignment Change for Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.TeamAssignmentChange,
                    new Dictionary<string, string> { { "EMAIL", Email }, { "SESSIONDATE", SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", SessionUrl }, { "FIRSTNAME", FirstName }, { "LASTNAME", LastName }, { "FORMERTEAMASSIGNMENT", FormerTeamAssignment }, { "NEWTEAMASSIGNMENT", NewTeamAssignment } });

                _logger.LogInformation($"CommsHandler->Successfully sent Team Assignment Change email for: {Email}");
            }

            // Now send to all those that have notification 'All' set
            foreach (var email in Emails)
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                await _emailService.SendEmailAsync(email, $"Team Assignment Change for Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.TeamAssignmentChangeNotification,
                    new Dictionary<string, string> { { "EMAIL", email }, { "SESSIONDATE", SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", SessionUrl }, { "FIRSTNAME", FirstName }, { "LASTNAME", LastName }, { "FORMERTEAMASSIGNMENT", FormerTeamAssignment }, { "NEWTEAMASSIGNMENT", NewTeamAssignment } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {Emails.Count()} Team Assignment Change emails for: {SessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Team Assignment Change email for: {Email}");

            throw;
        }
    }
}
