using HockeyPickup.Api;
using Microsoft.Extensions.Logging;

namespace HockeyPickup.Comms.Services;

public interface ICommsHandler
{
    Task SendRegistrationConfirmationEmail(string Email, string UserId, string FirstName, string LastName, string ConfirmationUrl);
    Task SendForgotPasswordEmail(string Email, string UserId, string FirstName, string LastName, string ResetUrl);
    Task SendRawContentEmail(string Subject, string RawContent);
    Task SendCreateSessionEmails(ICollection<string> notificationEmails, DateTime SessionDate, string SessionUrl, string Note, string CreatedByName);
    Task SendTeamAssignmentChangeEmail(string Email, NotificationPreference NotificationPreference, ICollection<string> notificationEmails, DateTime SessionDate, string SessionUrl, string FirstName, string LastName, string FormerTeamAssignment, string NewTeamAssignment);
    Task SendBuyerSellerMatchedEmails(string buyerEmail, NotificationPreference buyerNotificationPreference, string sellerEmail, NotificationPreference sellerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string buyerFirstName, string buyerLastName, string sellerFirstName, string sellerLastName);
    Task SendAddedToBuyQueueEmails(string buyerEmail, NotificationPreference buyerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string buyerFirstName, string buyerLastName);
    Task SendAddedToSellQueueEmails(string sellerEmail, NotificationPreference sellerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string sellerFirstName, string sellerLastName);
    Task SendCancelledBuyQueueEmails(string buyerEmail, NotificationPreference buyerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string buyerFirstName, string buyerLastName);
    Task SendCancelledSellQueueEmails(string sellerEmail, NotificationPreference sellerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string sellerFirstName, string sellerLastName);
    Task SendAddedToRosterEmails(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName);
    Task SendDeletedFromRosterEmails(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName);
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

            await _emailService.SendEmailAsync(alertEmail!, Subject, EmailTemplate.RawContent, new Dictionary<string, string> { { "RAWCONTENT", RawContent } });

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

    public async Task SendCreateSessionEmails(ICollection<string> notificationEmails, DateTime SessionDate, string SessionUrl, string Note, string CreatedByName)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Create Session email for: {SessionDate}");

            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(e, $"Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")} Created", EmailTemplate.CreateSession,
                    new Dictionary<string, string> { { "EMAIL", e }, { "SESSIONDATE", SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", SessionUrl }, { "NOTE", Note }, { "CREATEDBYNAME", CreatedByName } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {notificationEmails.Count()} Create Session emails for: {SessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending {notificationEmails.Count()} Create Session emails for: {SessionDate}");

            throw;
        }
    }

    public async Task SendTeamAssignmentChangeEmail(string Email, NotificationPreference NotificationPreference, ICollection<string> notificationEmails, DateTime SessionDate, string SessionUrl, string FirstName, string LastName, string FormerTeamAssignment, string NewTeamAssignment)
    {
        try
        {
            if (NotificationPreference != NotificationPreference.None)
            {
                _logger.LogInformation($"CommsHandler->Sending Team Assignment Change email for: {Email}");

                await _emailService.SendEmailAsync(Email, $"Team Assignment Change for Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.TeamAssignmentChange,
                    new Dictionary<string, string> { { "EMAIL", Email }, { "SESSIONDATE", SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", SessionUrl }, { "FIRSTNAME", FirstName }, { "LASTNAME", LastName }, { "FORMERTEAMASSIGNMENT", FormerTeamAssignment }, { "NEWTEAMASSIGNMENT", NewTeamAssignment } });

                _logger.LogInformation($"CommsHandler->Successfully sent Team Assignment Change email for: {Email}");
            }

            // Now send to all those that have notification 'All' set
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(e, $"Alert: Team Assignment Change for Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.TeamAssignmentChangeNotification,
                    new Dictionary<string, string> { { "EMAIL", e }, { "SESSIONDATE", SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", SessionUrl }, { "FIRSTNAME", FirstName }, { "LASTNAME", LastName }, { "FORMERTEAMASSIGNMENT", FormerTeamAssignment }, { "NEWTEAMASSIGNMENT", NewTeamAssignment } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {notificationEmails.Count()} Team Assignment Change emails for: {SessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Team Assignment Change email for: {Email}");

            throw;
        }
    }

    public async Task SendBuyerSellerMatchedEmails(string buyerEmail, NotificationPreference buyerNotificationPreference,
        string sellerEmail, NotificationPreference sellerNotificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string buyerFirstName, string buyerLastName, string sellerFirstName, string sellerLastName)
    {
        try
        {
            // Email to buyer
            if (buyerNotificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    buyerEmail,
                    $"Spot Bought for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.BoughtSpotBuyer,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", buyerEmail },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SELLERFIRSTNAME", sellerFirstName },
                    { "SELLERLASTNAME", sellerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Email to seller
            if (sellerNotificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    sellerEmail,
                    $"Spot Sold for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.BoughtSpotSeller,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", buyerEmail },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "BUERFIRSTNAME", sellerFirstName },
                    { "BUYERLASTNAME", sellerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: Spot Sold for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.BoughtSpotNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "BUYERFIRSTNAME", buyerFirstName },
                    { "BUYERLASTNAME", buyerLastName },
                    { "SELLERFIRSTNAME", sellerFirstName },
                    { "SELLERLASTNAME", sellerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending bought spot emails");
            throw;
        }
    }

    public async Task SendAddedToBuyQueueEmails(string buyerEmail, NotificationPreference buyerNotificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string buyerFirstName, string buyerLastName)
    {
        try
        {
            // Email to buyer
            if (buyerNotificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    buyerEmail,
                    $"Added to Buy Queue for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.AddedToBuyQueue,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", buyerEmail },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: New Buyer in Queue for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.AddedToBuyQueueNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "BUYERFIRSTNAME", buyerFirstName },
                    { "BUYERLASTNAME", buyerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending added to buy queue emails");
            throw;
        }
    }

    public async Task SendAddedToSellQueueEmails(string sellerEmail, NotificationPreference sellerNotificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string sellerFirstName, string sellerLastName)
    {
        try
        {
            // Email to seller
            if (sellerNotificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    sellerEmail,
                    $"Added to Sell Queue for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.AddedToSellQueue,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", sellerEmail },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: New Spot Available for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.AddedToSellQueueNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SELLERFIRSTNAME", sellerFirstName },
                    { "SELLERLASTNAME", sellerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending added to sell queue emails");
            throw;
        }
    }

    public async Task SendCancelledBuyQueueEmails(string buyerEmail, NotificationPreference buyerNotificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string buyerFirstName, string buyerLastName)
    {
        try
        {
            // Email to buyer
            if (buyerNotificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    buyerEmail,
                    $"Buy Request Cancelled for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.CancelledBuyQueue,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", buyerEmail },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: Buy Request Cancelled for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.CancelledBuyQueueNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "BUYERFIRSTNAME", buyerFirstName },
                    { "BUYERLASTNAME", buyerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending cancelled buy queue emails");
            throw;
        }
    }

    public async Task SendCancelledSellQueueEmails(string sellerEmail, NotificationPreference sellerNotificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string sellerFirstName, string sellerLastName)
    {
        try
        {
            // Email to seller
            if (sellerNotificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    sellerEmail,
                    $"Sell Request Cancelled for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.CancelledSellQueue,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", sellerEmail },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: Sell Request Cancelled for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.CancelledSellQueueNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SELLERFIRSTNAME", sellerFirstName },
                    { "SELLERLASTNAME", sellerLastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending cancelled sell queue emails");
            throw;
        }
    }

    public async Task SendAddedToRosterEmails(string email, NotificationPreference notificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string firstName, string lastName)
    {
        try
        {
            // Email to player
            if (notificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    email,
                    $"Added to Roster for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.AddedToRoster,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", email },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: Roster Update for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.AddedToRosterNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "FIRSTNAME", firstName },
                    { "LASTNAME", lastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending added to roster emails");
            throw;
        }
    }

    public async Task SendDeletedFromRosterEmails(string email, NotificationPreference notificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string firstName, string lastName)
    {
        try
        {
            // Email to player
            if (notificationPreference != NotificationPreference.None)
            {
                await _emailService.SendEmailAsync(
                    email,
                    $"Removed from Roster for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.DeletedFromRoster,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", email },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }

            // Notify everyone with NotificationPreference.All
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(
                    e,
                    $"Alert: Roster Update for {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}",
                    EmailTemplate.DeletedFromRosterNotification,
                    new Dictionary<string, string>
                    {
                    { "EMAIL", e },
                    { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                    { "FIRSTNAME", firstName },
                    { "LASTNAME", lastName },
                    { "SESSIONURL", sessionUrl }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending deleted from roster emails");
            throw;
        }
    }
}
