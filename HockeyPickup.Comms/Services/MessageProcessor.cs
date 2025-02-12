#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable CS8601 // Possible null reference assignment.
using Microsoft.Extensions.Logging;
using HockeyPickup.Api;
using HockeyPickup.Comms.Services;
using Newtonsoft.Json;

public interface IMessageProcessor
{
    Task ProcessMessageAsync(ServiceBusCommsMessage message);
    Task ProcessAddedPaymentMethod(ServiceBusCommsMessage message);
    Task ProcessSaveUser(ServiceBusCommsMessage message);
    Task ProcessForgotPassword(ServiceBusCommsMessage message);
    Task ProcessPhotoUploaded(ServiceBusCommsMessage message);
    Task ProcessRegisterConfirmation(ServiceBusCommsMessage message);
    Task ProcessSignedIn(ServiceBusCommsMessage message);
    Task ProcessCreateSession(ServiceBusCommsMessage message);
    Task ProcessTeamAssignmentChange(ServiceBusCommsMessage message);
    Task ProcessAddedToRoster(ServiceBusCommsMessage message);
    Task ProcessDeletedFromRoster(ServiceBusCommsMessage message);
    Task ProcessAddedToBuyQueue(ServiceBusCommsMessage message);
    Task ProcessAddedToSellQueue(ServiceBusCommsMessage message);
    Task ProcessBoughtSpotFromSeller(ServiceBusCommsMessage message);
    Task ProcessSoldSpotToBuyer(ServiceBusCommsMessage message);
    Task ProcessCancelledBuyQueue(ServiceBusCommsMessage message);
    Task ProcessCancelledSellQueue(ServiceBusCommsMessage message);

    bool ValidateAddedPaymentMethod(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName, out string paymentMethodType);
    bool ValidateSaveUser(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName);
    bool ValidatePhotoUploaded(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName);
    bool ValidateSignedInMessage(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName);
    bool ValidateRegisterConfirmationMessage(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string userId, out string firstName, out string lastName, out string confirmationUrl);
    bool ValidateForgotPasswordMessage(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string userId, out string firstName, out string lastName, out string resetUrl);
    bool ValidateCreateSessionMessage(ServiceBusCommsMessage message, out DateTime sessionDate, out string sessionUrl, out string note, out string createdByName);
    bool ValidateTeamAssignmentChange(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName, out DateTime sessionDate, out string sessionUrl, out string formerTeamAssignment, out string newTeamAssignment);
    bool ValidateUserMessage(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName);
    bool ValidateBuyerMessage(ServiceBusCommsMessage message, out string buyerEmail, out NotificationPreference buyerNotificationPreference, out string buyerFirstName, out string buyerLastName, out string teamAssignment);
    bool ValidateSellerMessage(ServiceBusCommsMessage message, out string sellerEmail, out NotificationPreference sellerNotificationPreference, out string sellerFirstName, out string sellerLastName, out string teamAssignment);
    bool ValidateBuySellMessage(ServiceBusCommsMessage message, out string buyerEmail, out NotificationPreference buyerNotificationPreference, out string sellerEmail, out NotificationPreference sellerNotificationPreference, out string buyerFirstName, out string buyerLastName, out string sellerFirstName, out string sellerLastName, out string teamAssignment);
}

public class MessageProcessor : IMessageProcessor
{
    private readonly ICommsHandler _commsHandler;
    private readonly ILogger<MessageProcessor> _logger;
    private readonly TelegramBot _telegramBot;
    private readonly bool _isDev;
    private readonly string _alertEmail;

    public MessageProcessor(ICommsHandler commsHandler, ILogger<MessageProcessor> logger, TelegramBot telegramBot)
    {
        _commsHandler = commsHandler;
        _logger = logger;
        _telegramBot = telegramBot;
        _isDev = Environment.GetEnvironmentVariable("ServiceBusCommsQueueName")!.Contains("-dev");
        _alertEmail = Environment.GetEnvironmentVariable("SignInAlertEmail")!;
    }

    public async Task ProcessMessageAsync(ServiceBusCommsMessage message)
    {
        _logger.LogInformation($"MessageProcessor->Processing message for communication event: {message.Metadata["CommunicationEventId"]}");

        // If this is a dev message queue, don't attempt to notify users of message. Clear that and just add one record.
        if (_isDev && message.NotificationEmails != null && message.NotificationEmails.Count > 1)
        {
            message.NotificationEmails.Clear();
            message.NotificationEmails.Add(_alertEmail);
        }

        switch (message.Metadata["Type"])
        {
            // User messages
            case "AddedPaymentMethod":
                await ProcessAddedPaymentMethod(message);
                break;

            case "SaveUser":
                await ProcessSaveUser(message);
                break;

            case "ForgotPassword":
                await ProcessForgotPassword(message);
                break;

            case "PhotoUploaded":
                await ProcessPhotoUploaded(message);
                break;

            case "RegisterConfirmation":
                await ProcessRegisterConfirmation(message);
                break;

            case "SignedIn":
                await ProcessSignedIn(message);
                break;

            case "CreateSession":
                await ProcessCreateSession(message);
                break;

            case "AddedToRoster":
                await ProcessAddedToRoster(message);
                break;

            case "DeletedFromRoster":
                await ProcessDeletedFromRoster(message);
                break;

            case "TeamAssignmentChange":
                await ProcessTeamAssignmentChange(message);
                break;

            case "AddedToBuyQueue":
                await ProcessAddedToBuyQueue(message);
                break;

            case "AddedToSellQueue":
                await ProcessAddedToSellQueue(message);
                break;

            case "BoughtSpotFromSeller":
                await ProcessBoughtSpotFromSeller(message);
                break;

            case "SoldSpotToBuyer":
                await ProcessSoldSpotToBuyer(message);
                break;

            case "CancelledBuyQueuePosition":
                await ProcessCancelledBuyQueue(message);
                break;

            case "CancelledSellQueuePosition":
                await ProcessCancelledSellQueue(message);
                break;

            default:
                await ProcessGenericMessage(message);
                break;
        }
    }

    public async Task ProcessGenericMessage(ServiceBusCommsMessage message)
    {
        await _telegramBot.SendChannelMessageAsync($"{message.Metadata["Type"]} Received\r\n\r\n" +
            $"CommunicationMethod:\r\n{JsonConvert.SerializeObject(message.CommunicationMethod, Formatting.Indented)}\r\n" +
            $"MessageData:\r\n{JsonConvert.SerializeObject(message.MessageData, Formatting.Indented)}\r\n" +
            $"RelatedEntities:\r\n{JsonConvert.SerializeObject(message.RelatedEntities, Formatting.Indented)}"
        );
    }

    public async Task ProcessTeamAssignmentChange(ServiceBusCommsMessage message)
    {
        if (!ValidateTeamAssignmentChange(message, out var email, out var notificationPreference, out var firstName, out var lastName, out var sessionDate, out var sessionUrl, out var formerTeamAssignment, out var newTeamAssignment))
        {
            throw new ArgumentException("Required data missing for ProcessTeamAssignmentChange message");
        }

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {firstName} {lastName} - Team Assignment Change from {formerTeamAssignment} to {newTeamAssignment}.");

        await _commsHandler.SendTeamAssignmentChangeEmail(email, notificationPreference, message.NotificationEmails, sessionDate, sessionUrl, firstName, lastName, formerTeamAssignment, newTeamAssignment);
    }

    public bool ValidateTeamAssignmentChange(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName, out DateTime SessionDate, out string SessionUrl, out string FormerTeamAssignment, out string NewTeamAssignment)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
            SessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
            SessionUrl = message.MessageData["SessionUrl"];
            FormerTeamAssignment = message.MessageData["FormerTeamAssignment"];
            NewTeamAssignment = message.MessageData["NewTeamAssignment"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            FirstName = string.Empty;
            LastName = string.Empty;
            SessionDate = DateTime.MinValue;
            SessionUrl = string.Empty;
            FormerTeamAssignment = string.Empty;
            NewTeamAssignment = string.Empty;

            return false;
        }

        return true;
    }

    public async Task ProcessAddedPaymentMethod(ServiceBusCommsMessage message)
    {
        if (!ValidateAddedPaymentMethod(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName, out var PaymentMethodType))
        {
            throw new ArgumentException("Required data missing for AddedPaymentMethod message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Added Payment Method Type {PaymentMethodType}");
    }

    public async Task ProcessSaveUser(ServiceBusCommsMessage message)
    {
        if (!ValidateSaveUser(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for SaveUser message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Saved User Preferences");
    }

    public bool ValidateAddedPaymentMethod(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName, out string PaymentMethodType)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
            PaymentMethodType = message.MessageData["PaymentMethodType"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            FirstName = string.Empty;
            LastName = string.Empty;
            PaymentMethodType = string.Empty;
            return false;
        }

        return true;
    }

    public bool ValidateSaveUser(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            FirstName = string.Empty;
            LastName = string.Empty;
            return false;
        }

        return true;
    }

    public async Task ProcessPhotoUploaded(ServiceBusCommsMessage message)
    {
        if (!ValidatePhotoUploaded(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for PhotoUploaded message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Uploaded Photo");
    }

    public bool ValidatePhotoUploaded(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            FirstName = string.Empty;
            LastName = string.Empty;
            return false;
        }

        return true;
    }

    public async Task ProcessCreateSession(ServiceBusCommsMessage message)
    {
        if (!ValidateCreateSessionMessage(message, out var SessionDate, out var SessionUrl, out var Note, out var CreatedByName))
        {
            throw new ArgumentException("Required data missing for CreateSession message");
        }

        await _commsHandler.SendCreateSessionEmails(message.NotificationEmails, SessionDate, SessionUrl, Note, CreatedByName);
    }

    public bool ValidateCreateSessionMessage(ServiceBusCommsMessage message, out DateTime SessionDate, out string SessionUrl, out string Note, out string CreatedByName)
    {
        try
        {
            SessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
            Note = message.MessageData["Note"];
            CreatedByName = message.MessageData["CreatedByName"];
            SessionUrl = message.MessageData["SessionUrl"];
        }
        catch
        {
            SessionDate = DateTime.MinValue;
            Note = string.Empty;
            CreatedByName = string.Empty;
            SessionUrl = string.Empty;
            return false;
        }

        return true;
    }

    public async Task ProcessSignedIn(ServiceBusCommsMessage message)
    {
        if (!ValidateSignedInMessage(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for SignedIn message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Signed In");
    }

    public bool ValidateSignedInMessage(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            FirstName = string.Empty;
            LastName = string.Empty;
            return false;
        }

        return true;
    }

    public async Task ProcessRegisterConfirmation(ServiceBusCommsMessage message)
    {
        if (!ValidateRegisterConfirmationMessage(message, out var Email, out var NotificationPreference, out var UserId, out var FirstName, out var LastName, out var ConfirmationUrl))
        {
            throw new ArgumentException("Required data missing for RegisterConfirmation message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Registered");

        await _commsHandler.SendRegistrationConfirmationEmail(Email, UserId, FirstName, LastName, ConfirmationUrl);
    }

    public bool ValidateRegisterConfirmationMessage(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string UserId, out string FirstName, out string LastName, out string ConfirmationUrl)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            UserId = message.RelatedEntities["UserId"];
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
            ConfirmationUrl = message.MessageData["ConfirmationUrl"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            UserId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            ConfirmationUrl = string.Empty;
            return false;
        }

        return true;
    }

    public async Task ProcessForgotPassword(ServiceBusCommsMessage message)
    {
        if (!ValidateForgotPasswordMessage(message, out var Email, out var NotificationPreference, out var UserId, out var FirstName, out var LastName, out var ResetUrl))
        {
            throw new ArgumentException("Required data missing for ForgotPassword message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Invoked Forgot Password");

        await _commsHandler.SendForgotPasswordEmail(Email, UserId, FirstName, LastName, ResetUrl);
    }

    public bool ValidateForgotPasswordMessage(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string UserId, out string FirstName, out string LastName, out string ResetUrl)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            UserId = message.RelatedEntities["UserId"];
            FirstName = message.RelatedEntities["FirstName"];
            LastName = message.RelatedEntities["LastName"];
            ResetUrl = message.MessageData["ResetUrl"];
        }
        catch
        {
            Email = string.Empty;
            NotificationPreference = NotificationPreference.None;
            UserId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            ResetUrl = string.Empty;
            return false;
        }

        return true;
    }

    public async Task ProcessAddedToRoster(ServiceBusCommsMessage message)
    {
        if (!ValidateUserMessage(message, out var email, out var notificationPreference, out var firstName, out var lastName))
        {
            throw new ArgumentException("Required data missing for AddedToRoster message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {firstName} {lastName} Added to Roster");

        await _commsHandler.SendAddedToRosterEmails(
            email,
            notificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            firstName,
            lastName
        );
    }

    public async Task ProcessDeletedFromRoster(ServiceBusCommsMessage message)
    {
        if (!ValidateUserMessage(message, out var email, out var notificationPreference, out var firstName, out var lastName))
        {
            throw new ArgumentException("Required data missing for DeletedFromRoster message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {firstName} {lastName} Removed from Roster.");

        await _commsHandler.SendDeletedFromRosterEmails(
            email,
            notificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            firstName,
            lastName
        );
    }

    public async Task ProcessAddedToBuyQueue(ServiceBusCommsMessage message)
    {
        if (!ValidateBuyerMessage(message, out var buyerEmail, out var buyerNotificationPreference, out var buyerFirstName, out var buyerLastName, out var teamAssignment))
        {
            throw new ArgumentException("Required data missing for AddedToBuyQueue message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {buyerFirstName} {buyerLastName} Added to Buy Queue.");

        await _commsHandler.SendAddedToBuyQueueEmails(
            buyerEmail,
            buyerNotificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            buyerFirstName,
            buyerLastName
        );
    }

    public async Task ProcessAddedToSellQueue(ServiceBusCommsMessage message)
    {
        if (!ValidateSellerMessage(message, out var sellerEmail, out var sellerNotificationPreference, out var sellerFirstName, out var sellerLastName, out var teamAssignment))
        {
            throw new ArgumentException("Required data missing for AddedToSellQueue message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {sellerFirstName} {sellerLastName} Added to Sell Queue.");

        await _commsHandler.SendAddedToSellQueueEmails(
            sellerEmail,
            sellerNotificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            sellerFirstName,
            sellerLastName
        );
    }

    public async Task ProcessBoughtSpotFromSeller(ServiceBusCommsMessage message)
    {
        if (!ValidateBuySellMessage(message, out var buyerEmail, out var buyerNotificationPreference, out var sellerEmail, out var sellerNotificationPreference, out var buyerFirstName, out var buyerLastName, out var sellerFirstName, out var sellerLastName, out var teamAssignment))
        {
            throw new ArgumentException("Required data missing for BoughtSpotFromSeller message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {buyerFirstName} {buyerLastName} BOUGHT Spot from {sellerFirstName} {sellerLastName}. Team Assignment: {teamAssignment}.");

        await _commsHandler.SendBuyerSellerMatchedEmails(
            buyerEmail,
            buyerNotificationPreference,
            sellerEmail,
            sellerNotificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            buyerFirstName,
            buyerLastName,
            sellerFirstName,
            sellerLastName,
            teamAssignment
        );
    }

    public async Task ProcessSoldSpotToBuyer(ServiceBusCommsMessage message)
    {
        if (!ValidateBuySellMessage(message, out var buyerEmail, out var buyerNotificationPreference, out var sellerEmail, out var sellerNotificationPreference, out var buyerFirstName, out var buyerLastName, out var sellerFirstName, out var sellerLastName, out var teamAssignment))
        {
            throw new ArgumentException("Required data missing for SoldSpotToBuyer message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {sellerFirstName} {sellerLastName} Sold Spot to {buyerFirstName} {buyerLastName}."
        );

        await _commsHandler.SendBuyerSellerMatchedEmails(
            buyerEmail,
            buyerNotificationPreference,
            sellerEmail,
            sellerNotificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            buyerFirstName,
            buyerLastName,
            sellerFirstName,
            sellerLastName,
            teamAssignment
        );
    }

    public async Task ProcessCancelledBuyQueue(ServiceBusCommsMessage message)
    {
        if (!ValidateBuyerMessage(message, out var buyerEmail, out var buyerNotificationPreference, out var buyerFirstName, out var buyerLastName, out var teamAssignment))
        {
            throw new ArgumentException("Required data missing for CancelledBuyQueue message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {buyerFirstName} {buyerLastName} Cancelled Buy Request.");

        await _commsHandler.SendCancelledBuyQueueEmails(
            buyerEmail,
            buyerNotificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            buyerFirstName,
            buyerLastName
        );
    }

    public async Task ProcessCancelledSellQueue(ServiceBusCommsMessage message)
    {
        if (!ValidateSellerMessage(message, out var sellerEmail, out var sellerNotificationPreference, out var sellerFirstName, out var sellerLastName, out var teamAssignment))
        {
            throw new ArgumentException("Required data missing for CancelledSellQueue message");
        }

        var sessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
        var sessionUrl = message.MessageData["SessionUrl"];

        await _telegramBot.SendChannelMessageAsync($"Session: {sessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}. {sellerFirstName} {sellerLastName} Cancelled Sell Request.");

        await _commsHandler.SendCancelledSellQueueEmails(
            sellerEmail,
            sellerNotificationPreference,
            message.NotificationEmails,
            sessionDate,
            sessionUrl,
            sellerFirstName,
            sellerLastName
        );
    }

    public bool ValidateUserMessage(ServiceBusCommsMessage message, out string email, out NotificationPreference notificationPreference, out string firstName, out string lastName)
    {
        try
        {
            email = message.CommunicationMethod["Email"];
            notificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            firstName = message.RelatedEntities["FirstName"];
            lastName = message.RelatedEntities["LastName"];
            return true;
        }
        catch
        {
            email = string.Empty;
            notificationPreference = NotificationPreference.None;
            firstName = string.Empty;
            lastName = string.Empty;
            return false;
        }
    }

    public bool ValidateBuyerMessage(ServiceBusCommsMessage message, out string buyerEmail, out NotificationPreference buyerNotificationPreference, out string buyerFirstName, out string buyerLastName, out string teamAssignment)
    {
        try
        {
            buyerEmail = message.CommunicationMethod["BuyerEmail"];
            buyerNotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["BuyerNotificationPreference"]);
            buyerFirstName = message.RelatedEntities["BuyerFirstName"];
            buyerLastName = message.RelatedEntities["BuyerLastName"];
            teamAssignment = message.RelatedEntities["TeamAssignment"];
            return true;
        }
        catch
        {
            buyerEmail = string.Empty;
            buyerNotificationPreference = NotificationPreference.None;
            buyerFirstName = string.Empty;
            buyerLastName = string.Empty;
            teamAssignment = TeamAssignment.TBD.ToString();
            return false;
        }
    }

    public bool ValidateSellerMessage(ServiceBusCommsMessage message, out string sellerEmail, out NotificationPreference sellerNotificationPreference, out string sellerFirstName, out string sellerLastName, out string teamAssignment)
    {
        try
        {
            sellerEmail = message.CommunicationMethod["SellerEmail"];
            sellerNotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["SellerNotificationPreference"]);
            sellerFirstName = message.RelatedEntities["SellerFirstName"];
            sellerLastName = message.RelatedEntities["SellerLastName"];
            teamAssignment = message.RelatedEntities["TeamAssignment"];
            return true;
        }
        catch
        {
            sellerEmail = string.Empty;
            sellerNotificationPreference = NotificationPreference.None;
            sellerFirstName = string.Empty;
            sellerLastName = string.Empty;
            teamAssignment = TeamAssignment.TBD.ToString();
            return false;
        }
    }

    public bool ValidateBuySellMessage(ServiceBusCommsMessage message, out string buyerEmail,
        out NotificationPreference buyerNotificationPreference, out string sellerEmail,
        out NotificationPreference sellerNotificationPreference, out string buyerFirstName, out string buyerLastName,
        out string sellerFirstName, out string sellerLastName, out string teamAssignment)
    {
        sellerEmail = null!;
        sellerNotificationPreference = default;
        sellerFirstName = null!;
        sellerLastName = null!;
        return ValidateBuyerMessage(message, out buyerEmail, out buyerNotificationPreference, out buyerFirstName, out buyerLastName, out teamAssignment) &&
            ValidateSellerMessage(message, out sellerEmail, out sellerNotificationPreference, out sellerFirstName, out sellerLastName, out teamAssignment);
    }
}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS8601 // Possible null reference assignment.
