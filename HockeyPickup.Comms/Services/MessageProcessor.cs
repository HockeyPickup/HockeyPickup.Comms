#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable CS8601 // Possible null reference assignment.
using Microsoft.Extensions.Logging;
using HockeyPickup.Api;
using HockeyPickup.Comms.Services;
using Newtonsoft.Json;

public interface IMessageProcessor
{
    Task ProcessMessageAsync(ServiceBusCommsMessage message);
}

public class MessageProcessor : IMessageProcessor
{
    private readonly ICommsHandler _commsHandler;
    private readonly ILogger<MessageProcessor> _logger;
    private readonly TelegramBot _telegramBot;

    public MessageProcessor(ICommsHandler commsHandler, ILogger<MessageProcessor> logger, TelegramBot telegramBot)
    {
        _commsHandler = commsHandler;
        _logger = logger;
        _telegramBot = telegramBot;
    }

    public async Task ProcessMessageAsync(ServiceBusCommsMessage message)
    {
        _logger.LogInformation($"MessageProcessor->Processing message for communication event: {message.Metadata["CommunicationEventId"]}");

        switch (message.Metadata["Type"])
        {
            // User messages
            case "AddedPaymentMethod":
                await ProcessAddedPaymentMethod(message);
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

            // Session messages
            case "AddedToRoster":
                await ProcessGenericMessage(message);
                break;

            case "CreateSession":
                await ProcessCreateSession(message);
                break;

            case "DeletedFromRoster":
                await ProcessGenericMessage(message);
                break;

            case "TeamAssignmentChange":
                await ProcessTeamAssignmentChange(message);
                break;

            // BuySell messages
            case "AddedToBuyQueue":
            case "AddedToSellQueue":
            case "BoughtSpotFromBuyer":
            case "SoldSpotToBuyer":
            case "CancelledBuyQueuePosition":
            case "CancelledSellQueuePosition":
                await ProcessGenericMessage(message);
                break;

            default:
                throw new ArgumentException($"Unknown message type: {message.Metadata["Type"]}");
        }
    }

    private async Task ProcessGenericMessage(ServiceBusCommsMessage message)
    {
        await _telegramBot.SendChannelMessageAsync($"{message.Metadata["Type"]} Received\r\n\r\n" +
            $"CommunicationMethod:\r\n{JsonConvert.SerializeObject(message.CommunicationMethod, Formatting.Indented)}\r\n" +
            $"MessageData:\r\n{JsonConvert.SerializeObject(message.MessageData, Formatting.Indented)}\r\n" +
            $"RelatedEntities:\r\n{JsonConvert.SerializeObject(message.RelatedEntities, Formatting.Indented)}"
        );
    }

    private async Task ProcessTeamAssignmentChange(ServiceBusCommsMessage message)
    {
        if (!ValidateTeamAssignmentChange(message, out var Email, out var NotificationPreference, out var Emails, out var FirstName, out var LastName, out var SessionDate, out var SessionUrl, out var FormerTeamAssignment, out var NewTeamAssignment))
        {
            throw new ArgumentException("Required data missing for ProcessTeamAssignmentChange message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} - Team Assignment Change from {FormerTeamAssignment} to {NewTeamAssignment} for Session {SessionDate.ToString("dddd, MM/dd/yyyy, HH:mm")}");

        await _commsHandler.SendTeamAssignmentChangeEmail(Email, NotificationPreference, Emails, SessionDate, SessionUrl, FirstName, LastName, FormerTeamAssignment, NewTeamAssignment);
    }

    private bool ValidateTeamAssignmentChange(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out ICollection<string> Emails, out string FirstName, out string LastName, out DateTime SessionDate, out string SessionUrl, out string FormerTeamAssignment, out string NewTeamAssignment)
    {
        try
        {
            Email = message.CommunicationMethod["Email"];
            NotificationPreference = Enum.Parse<NotificationPreference>(message.CommunicationMethod["NotificationPreference"]);
            Emails = message.NotificationEmails;
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
            Emails = Array.Empty<string>();
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

    private async Task ProcessAddedPaymentMethod(ServiceBusCommsMessage message)
    {
        if (!ValidateAddedPaymentMethod(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName, out var PaymentMethodType))
        {
            throw new ArgumentException("Required data missing for AddedPaymentMethod message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Added Payment Method Type {PaymentMethodType}");
    }

    private bool ValidateAddedPaymentMethod(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName, out string PaymentMethodType)
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

    private async Task ProcessPhotoUploaded(ServiceBusCommsMessage message)
    {
        if (!ValidatePhotoUploaded(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for PhotoUploaded message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Uploaded Photo");
    }

    private bool ValidatePhotoUploaded(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName)
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

    private async Task ProcessCreateSession(ServiceBusCommsMessage message)
    {
        if (!ValidateCreateSessionMessage(message, out var Emails, out var SessionDate, out var SessionUrl, out var Note, out var CreatedByName))
        {
            throw new ArgumentException("Required data missing for CreateSession message");
        }

        await _commsHandler.SendCreateSessionEmails(Emails, SessionDate, SessionUrl, Note, CreatedByName);
    }

    private bool ValidateCreateSessionMessage(ServiceBusCommsMessage message, out ICollection<string> Emails, out DateTime SessionDate, out string SessionUrl, out string Note, out string CreatedByName)
    {
        try
        {
            Emails = message.NotificationEmails;
            SessionDate = DateTime.Parse(message.MessageData["SessionDate"]);
            Note = message.MessageData["Note"];
            CreatedByName = message.MessageData["CreatedByName"];
            SessionUrl = message.MessageData["SessionUrl"];
        }
        catch
        {
            Emails = Array.Empty<string>();
            SessionDate = DateTime.MinValue;
            Note = string.Empty;
            CreatedByName = string.Empty;
            SessionUrl = string.Empty;
            return false;
        }

        return true;
    }

    private async Task ProcessSignedIn(ServiceBusCommsMessage message)
    {
        if (!ValidateSignedInMessage(message, out var Email, out var NotificationPreference, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for SignedIn message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Signed In");
    }

    private bool ValidateSignedInMessage(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string FirstName, out string LastName)
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

    private async Task ProcessRegisterConfirmation(ServiceBusCommsMessage message)
    {
        if (!ValidateRegisterConfirmationMessage(message, out var Email, out var NotificationPreference, out var UserId, out var FirstName, out var LastName, out var ConfirmationUrl))
        {
            throw new ArgumentException("Required data missing for RegisterConfirmation message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Registered");

        await _commsHandler.SendRegistrationConfirmationEmail(Email, UserId, FirstName, LastName, ConfirmationUrl);
    }

    private bool ValidateRegisterConfirmationMessage(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string UserId, out string FirstName, out string LastName, out string ConfirmationUrl)
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

    private async Task ProcessForgotPassword(ServiceBusCommsMessage message)
    {
        if (!ValidateForgotPasswordMessage(message, out var Email, out var NotificationPreference, out var UserId, out var FirstName, out var LastName, out var ResetUrl))
        {
            throw new ArgumentException("Required data missing for ForgotPassword message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Invoked Forgot Password");

        await _commsHandler.SendForgotPasswordEmail(Email, UserId, FirstName, LastName, ResetUrl);
    }

    private bool ValidateForgotPasswordMessage(ServiceBusCommsMessage message, out string Email, out NotificationPreference NotificationPreference, out string UserId, out string FirstName, out string LastName, out string ResetUrl)
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
}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS8601 // Possible null reference assignment.
