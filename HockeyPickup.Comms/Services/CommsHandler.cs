using HockeyPickup.Api;
using Microsoft.Extensions.Logging;

namespace HockeyPickup.Comms.Services;

public interface ICommsHandler
{
    Task SendRegistrationConfirmationEmail(string email, string userId, string firstName, string lastName, string confirmationUrl);
    Task SendForgotPasswordEmail(string email, string userId, string firstName, string lastName, string resetUrl);
    Task SendRawContentEmail(string subject, string rawContent);
    Task SendCreateSessionEmails(ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string note, string createdByName);
    Task SendTeamAssignmentChangeEmail(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName, string formerTeamAssignment, string newTeamAssignment);
    Task SendBuyerSellerMatchedEmails(string buyerEmail, NotificationPreference buyerNotificationPreference, string sellerEmail, NotificationPreference sellerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string buyerFirstName, string buyerLastName, string sellerFirstName, string sellerLastName, string teamAssignment);
    Task SendAddedToBuyQueueEmails(string buyerEmail, NotificationPreference buyerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string buyerFirstName, string buyerLastName);
    Task SendAddedToSellQueueEmails(string sellerEmail, NotificationPreference sellerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string sellerFirstName, string sellerLastName);
    Task SendCancelledBuyQueueEmails(string buyerEmail, NotificationPreference buyerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string buyerFirstName, string buyerLastName);
    Task SendCancelledSellQueueEmails(string sellerEmail, NotificationPreference sellerNotificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string sellerFirstName, string sellerLastName);
    Task SendAddedToRosterEmails(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName);
    Task SendDeletedFromRosterEmails(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName);
    Task SendProcessPlayingStatusChangeEmail(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName, string previousPlayingStatusString, string updatedPlayingStatusString);
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

    public async Task SendRawContentEmail(string subject, string rawContent)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Raw Content Email");

            var alertEmail = Environment.GetEnvironmentVariable("SignInAlertEmail")!;

            await _emailService.SendEmailAsync(alertEmail, subject, EmailTemplate.RawContent, new Dictionary<string, string> { { "RAWCONTENT", rawContent } });

            _logger.LogInformation($"CommsHandler->Successfully sent Raw Content email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Successfully sent Raw Content email");

            throw;
        }
    }

    public async Task SendForgotPasswordEmail(string email, string userId, string firstName, string lastName, string resetUrl)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Forgot Password email for: {email}");

            await _emailService.SendEmailAsync(email, "Reset Password Request", EmailTemplate.ForgotPassword,
                new Dictionary<string, string> { { "EMAIL", email }, { "FIRSTNAME", firstName }, { "LASTNAME", lastName }, { "RESET_URL", resetUrl } });

            _logger.LogInformation($"CommsHandler->Successfully sent Forgot Password email for: {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Forgot Password email for: {email}");

            throw;
        }
    }

    public async Task SendRegistrationConfirmationEmail(string email, string userId, string firstName, string lastName, string confirmationUrl)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Registration Confirmation email for: {email}");

            await _emailService.SendEmailAsync(email, "Registration Confirmation", EmailTemplate.Register,
                new Dictionary<string, string> { { "EMAIL", email }, { "FIRSTNAME", firstName }, { "LASTNAME", lastName }, { "CONFIRMATION_URL", confirmationUrl } });

            _logger.LogInformation($"CommsHandler->Successfully sent Registration Confirmation email for: {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Registration Confirmation email for: {email}");

            throw;
        }
    }

    public async Task SendCreateSessionEmails(ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string note, string createdByName)
    {
        try
        {
            _logger.LogInformation($"CommsHandler->Sending Create Session email for: {sessionDate}");

            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(e, $"Session {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")} Created", EmailTemplate.CreateSessionNotification,
                    new Dictionary<string, string> { { "EMAIL", e }, { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", sessionUrl }, { "NOTE", note }, { "CREATEDBYNAME", createdByName } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {notificationEmails.Count()} Create Session emails for: {sessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending {notificationEmails.Count()} Create Session emails for: {sessionDate}");

            throw;
        }
    }

    public async Task SendTeamAssignmentChangeEmail(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName, string formerTeamAssignment, string newTeamAssignment)
    {
        try
        {
            if (notificationPreference != NotificationPreference.None)
            {
                _logger.LogInformation($"CommsHandler->Sending Team Assignment Change email for: {email}");

                await _emailService.SendEmailAsync(email, $"Team Assignment Change for Session {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.TeamAssignmentChange,
                    new Dictionary<string, string> { { "EMAIL", email }, { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", sessionUrl }, { "FIRSTNAME", firstName }, { "LASTNAME", lastName }, { "FORMERTEAMASSIGNMENT", formerTeamAssignment }, { "NEWTEAMASSIGNMENT", newTeamAssignment } });

                _logger.LogInformation($"CommsHandler->Successfully sent Team Assignment Change email for: {email}");
            }

            // Now send to all those that have notification 'All' set
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(e, $"Alert: Team Assignment Change for Session {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.TeamAssignmentChangeNotification,
                    new Dictionary<string, string> { { "EMAIL", e }, { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", sessionUrl }, { "FIRSTNAME", firstName }, { "LASTNAME", lastName }, { "FORMERTEAMASSIGNMENT", formerTeamAssignment }, { "NEWTEAMASSIGNMENT", newTeamAssignment } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {notificationEmails.Count()} Team Assignment Change emails for: {sessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Team Assignment Change email for: {email}");

            throw;
        }
    }

    public async Task SendProcessPlayingStatusChangeEmail(string email, NotificationPreference notificationPreference, ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl, string firstName, string lastName, string previousPlayingStatusString, string updatedPlayingStatusString)
    {
        try
        {
            if (notificationPreference != NotificationPreference.None)
            {
                _logger.LogInformation($"CommsHandler->Sending Playing Status Change email for: {email}");

                await _emailService.SendEmailAsync(email, $"Playing Status Change for Session {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.PlayingStatusChange,
                    new Dictionary<string, string> { { "EMAIL", email }, { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", sessionUrl }, { "FIRSTNAME", firstName }, { "LASTNAME", lastName }, { "PREVIOUSPLAYINGSTATUSSTRING", previousPlayingStatusString }, { "UPDATEDPLAYINGSTATUSSTRING", updatedPlayingStatusString } });

                _logger.LogInformation($"CommsHandler->Successfully sent Playing Status Change email for: {email}");
            }

            // Now send to all those that have notification 'All' set
            foreach (var e in notificationEmails)
            {
                await _emailService.SendEmailAsync(e, $"Alert: Playing Status Change for Session {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}", EmailTemplate.PlayingStatusChangeNotification,
                    new Dictionary<string, string> { { "EMAIL", e }, { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") }, { "SESSION_URL", sessionUrl }, { "FIRSTNAME", firstName }, { "LASTNAME", lastName }, { "PREVIOUSPLAYINGSTATUSSTRING", previousPlayingStatusString }, { "UPDATEDPLAYINGSTATUSSTRING", updatedPlayingStatusString } });
            }

            _logger.LogInformation($"CommsHandler->Successfully sent {notificationEmails.Count()} Playing Status Change emails for: {sessionDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CommsHandler->Error sending Playing Status Change email for: {email}");

            throw;
        }
    }

    public async Task SendBuyerSellerMatchedEmails(string buyerEmail, NotificationPreference buyerNotificationPreference,
        string sellerEmail, NotificationPreference sellerNotificationPreference,
        ICollection<string> notificationEmails, DateTime sessionDate, string sessionUrl,
        string buyerFirstName, string buyerLastName, string sellerFirstName, string sellerLastName, string teamAssignment)
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
                        { "BUYERFIRSTNAME", buyerFirstName },
                        { "BUYERLASTNAME", buyerLastName },
                        { "SELLERFIRSTNAME", sellerFirstName },
                        { "SELLERLASTNAME", sellerLastName },
                        { "SESSIONURL", sessionUrl },
                        { "TEAMASSIGNMENT", teamAssignment }
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
                        { "EMAIL", sellerEmail },
                        { "SESSIONDATE", sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm") },
                        { "BUYERFIRSTNAME", buyerFirstName },
                        { "BUYERLASTNAME", buyerLastName },
                        { "SELLERFIRSTNAME", sellerFirstName },
                        { "SELLERLASTNAME", sellerLastName },
                        { "SESSIONURL", sessionUrl },
                        { "TEAMASSIGNMENT", teamAssignment }
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
                        { "SESSIONURL", sessionUrl },
                        { "TEAMASSIGNMENT", teamAssignment }
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
                        { "BUYERFIRSTNAME", buyerFirstName },
                        { "BUYERLASTNAME", buyerLastName },
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
                        { "SELLERFIRSTNAME", sellerFirstName },
                        { "SELLERLASTNAME", sellerLastName },
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
