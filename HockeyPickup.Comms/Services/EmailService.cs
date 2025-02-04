using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Reflection;

namespace HockeyPickup.Comms.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, EmailTemplate template, Dictionary<string, string> tokens);
}

public enum EmailTemplate
{
    SignedIn,
    Register,
    ForgotPassword,
    RawContent,
    CreateSessionNotification,
    TeamAssignmentChange,
    TeamAssignmentChangeNotification,
    BoughtSpotBuyer,
    BoughtSpotSeller,
    BoughtSpotNotification,
    AddedToBuyQueue,
    AddedToBuyQueueNotification,
    AddedToSellQueue,
    AddedToSellQueueNotification,
    CancelledBuyQueue,
    CancelledBuyQueueNotification,
    CancelledSellQueue,
    CancelledSellQueueNotification,
    AddedToRoster,
    AddedToRosterNotification,
    DeletedFromRoster,
    DeletedFromRosterNotification,
    // Add more as needed
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly Dictionary<EmailTemplate, (string File, HashSet<string> RequiredTokens)> _templateConfig;
    private readonly bool isLocalhost;
    private readonly string alertEmail;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        _templateConfig = new Dictionary<EmailTemplate, (string, HashSet<string>)>
        {
            {
                EmailTemplate.SignedIn,
                ("signed_in.txt", new HashSet<string> { "EMAIL" })
            },
            {
                EmailTemplate.RawContent,
                ("raw_content.txt", new HashSet<string> { "RAWCONTENT" })
            },
            {
                EmailTemplate.Register,
                ("register.txt", new HashSet<string> { "EMAIL", "FIRSTNAME", "LASTNAME", "CONFIRMATION_URL" })
            },
            {
                EmailTemplate.ForgotPassword,
                ("forgot_password.txt", new HashSet<string> { "EMAIL", "FIRSTNAME", "RESET_URL" })
            },
            {
                EmailTemplate.CreateSessionNotification,
                ("create_session_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSION_URL", "NOTE", "CREATEDBYNAME" })
            },
            {
                EmailTemplate.TeamAssignmentChange,
                ("team_assignment_change.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSION_URL", "FIRSTNAME", "LASTNAME", "FORMERTEAMASSIGNMENT", "NEWTEAMASSIGNMENT" })
            },
            {
                EmailTemplate.TeamAssignmentChangeNotification,
                ("team_assignment_change_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSION_URL", "FIRSTNAME", "LASTNAME", "FORMERTEAMASSIGNMENT", "NEWTEAMASSIGNMENT" })
            },
            {
                EmailTemplate.BoughtSpotBuyer,
                ("bought_spot_buyer.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SELLERFIRSTNAME", "SELLERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.BoughtSpotSeller,
                ("bought_spot_seller.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "BUYERFIRSTNAME", "BUYERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.BoughtSpotNotification,
                ("bought_spot_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "BUYERFIRSTNAME", "BUYERLASTNAME", "SELLERFIRSTNAME", "SELLERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.AddedToBuyQueue,
                ("added_to_buy_queue.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSIONURL" })
            },
            {
                EmailTemplate.AddedToBuyQueueNotification,
                ("added_to_buy_queue_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "BUYERFIRSTNAME", "BUYERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.AddedToSellQueue,
                ("added_to_sell_queue.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSIONURL" })
            },
            {
                EmailTemplate.AddedToSellQueueNotification,
                ("added_to_sell_queue_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SELLERFIRSTNAME", "SELLERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.CancelledBuyQueue,
                ("cancelled_buy_queue.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSIONURL" })
            },
            {
                EmailTemplate.CancelledBuyQueueNotification,
                ("cancelled_buy_queue_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "BUYERFIRSTNAME", "BUYERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.CancelledSellQueue,
                ("cancelled_sell_queue.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSIONURL" })
            },
            {
                EmailTemplate.CancelledSellQueueNotification,
                ("cancelled_sell_queue_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SELLERFIRSTNAME", "SELLERLASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.AddedToRoster,
                ("added_to_roster.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSIONURL" })
            },
            {
                EmailTemplate.AddedToRosterNotification,
                ("added_to_roster_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "FIRSTNAME", "LASTNAME", "SESSIONURL" })
            },
            {
                EmailTemplate.DeletedFromRoster,
                ("deleted_from_roster.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "SESSIONURL" })
            },
            {
                EmailTemplate.DeletedFromRosterNotification,
                ("deleted_from_roster_notification.txt", new HashSet<string> { "EMAIL", "SESSIONDATE", "FIRSTNAME", "LASTNAME", "SESSIONURL" })
            },
        };
        var baseApiUrl = Environment.GetEnvironmentVariable("BaseApiUrl");
        if (baseApiUrl!.Contains("localhost"))
        {
            isLocalhost = true;
        }
        alertEmail = Environment.GetEnvironmentVariable("SignInAlertEmail")!;
    }

    public async Task SendEmailAsync(string to, string subject, EmailTemplate template, Dictionary<string, string> tokens)
    {
        try
        {
            if (!_templateConfig.TryGetValue(template, out var config))
            {
                throw new ArgumentException($"Template not configured: {template}");
            }

            // Validate all required tokens are present
            var missingTokens = config.RequiredTokens.Where(t => !tokens.ContainsKey(t));
            if (missingTokens.Any())
            {
                throw new ArgumentException($"Missing required tokens: {string.Join(", ", missingTokens)}");
            }

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"HockeyPickup.Comms.email_templates.{config.File}");
            if (stream == null)
            {
                throw new ArgumentException($"Missing template: HockeyPickup.Comms.email_templates.{config.File}");
            }
            using var reader = new StreamReader(stream);
            var body = await reader.ReadToEndAsync();
            foreach (var token in tokens)
            {
                body = body.Replace($"{{{{{token.Key}}}}}", token.Value);
            }

            var message = new SendGridMessage();
            message.SetFrom(new EmailAddress(Environment.GetEnvironmentVariable("SendGridFromAddress")));

            // When running locally, override the 'to'. NEVER send an email to a real user
            if (isLocalhost)
            {
                to = alertEmail;
            }
            message.AddTo(to);
            message.SetSubject(subject);

            message.AddContent(MimeType.Html, body);

            var client = new SendGridClient(Environment.GetEnvironmentVariable("SendGridApiKey"));

            var response = await client.SendEmailAsync(message);

            _logger.LogInformation($"EmailService->Email sent successfully to: {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"EmailService->Error sending email to: {to}");
            throw;
        }
    }
}
