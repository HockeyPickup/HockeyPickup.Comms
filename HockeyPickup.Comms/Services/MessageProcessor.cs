#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable CS8601 // Possible null reference assignment.
using Microsoft.Extensions.Logging;
using HockeyPickup.Api;
using HockeyPickup.Comms.Services;

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
            case "SignedIn":
                await ProcessSignedIn(message);
                break;

            case "RegisterConfirmation":
                await ProcessRegisterConfirmation(message);
                break;

            case "ForgotPassword":
                await ProcessForgotPassword(message);
                break;

            case "CreateSession":
                await ProcessCreateSession(message);
                break;

            case "PhotoUploaded":
                await ProcessPhotoUploaded(message);
                break;

            case "AddedPaymentMethod":
                await ProcessAddedPaymentMethod(message);
                break;

            default:
                throw new ArgumentException($"Unknown message type: {message.Metadata["Type"]}");
        }
    }

    private async Task ProcessAddedPaymentMethod(ServiceBusCommsMessage message)
    {
        if (!ValidateAddedPaymentMethod(message, out var Email, out var FirstName, out var LastName, out var PaymentMethodType))
        {
            throw new ArgumentException("Required data missing for AddedPaymentMethod message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Added Payment Method Type {PaymentMethodType}");
    }

    private bool ValidateAddedPaymentMethod(ServiceBusCommsMessage message, out string Email, out string FirstName, out string LastName, out string PaymentMethodType)
    {
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PaymentMethodType = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out Email) &&
               message.RelatedEntities.TryGetValue("FirstName", out FirstName) &&
               message.RelatedEntities.TryGetValue("LastName", out LastName) &&
               message.MessageData.TryGetValue("PaymentMethodType", out PaymentMethodType);
    }

    private async Task ProcessPhotoUploaded(ServiceBusCommsMessage message)
    {
        if (!ValidatePhotoUploaded(message, out var Email, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for PhotoUploaded message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Uploaded Photo");
    }

    private bool ValidatePhotoUploaded(ServiceBusCommsMessage message, out string Email, out string FirstName, out string LastName)
    {
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out Email) &&
               message.RelatedEntities.TryGetValue("FirstName", out FirstName) &&
               message.RelatedEntities.TryGetValue("LastName", out LastName);
    }

    private async Task ProcessCreateSession(ServiceBusCommsMessage message)
    {
        if (!ValidateCreateSessionMessage(message, out var Emails, out var SessionDate, out var SessionUrl, out var Note, out var CreatedByName))
        {
            throw new ArgumentException("Required data missing for CreateSession message");
        }

        await _commsHandler.SendCreateSessionEmails(Emails, SessionDate, SessionUrl, Note, CreatedByName);
    }

    private bool ValidateCreateSessionMessage(ServiceBusCommsMessage message, out string[] Emails, out DateTime SessionDate, out string SessionUrl, out string Note, out string CreatedByName)
    {
        try
        {
            Emails = message.RelatedEntities.Values.ToArray();
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
        if (!ValidateSignedInMessage(message, out var Email, out var FirstName, out var LastName))
        {
            throw new ArgumentException("Required data missing for SignedIn message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Signed In");
    }

    private bool ValidateSignedInMessage(ServiceBusCommsMessage message, out string Email, out string FirstName, out string LastName)
    {
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out Email) &&
               message.RelatedEntities.TryGetValue("FirstName", out FirstName) &&
               message.RelatedEntities.TryGetValue("LastName", out LastName);
    }

    private async Task ProcessRegisterConfirmation(ServiceBusCommsMessage message)
    {
        if (!ValidateRegisterConfirmationMessage(message, out var Email, out var UserId, out var FirstName, out var LastName, out var ConfirmationUrl))
        {
            throw new ArgumentException("Required data missing for RegisterConfirmation message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Registered");

        await _commsHandler.SendRegistrationConfirmationEmail(Email, UserId, FirstName, LastName, ConfirmationUrl);
    }

    private bool ValidateRegisterConfirmationMessage(ServiceBusCommsMessage message, out string Email, out string UserId, out string FirstName, out string LastName, out string ConfirmationUrl)
    {
        Email = string.Empty;
        UserId = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        ConfirmationUrl = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out Email) &&
               message.MessageData?.TryGetValue("ConfirmationUrl", out ConfirmationUrl) == true &&
               message.RelatedEntities.TryGetValue("UserId", out UserId) &&
               message.RelatedEntities.TryGetValue("FirstName", out FirstName) &&
               message.RelatedEntities.TryGetValue("LastName", out LastName);

    }

    private async Task ProcessForgotPassword(ServiceBusCommsMessage message)
    {
        if (!ValidateForgotPasswordMessage(message, out var Email, out var UserId, out var FirstName, out var LastName, out var ResetUrl))
        {
            throw new ArgumentException("Required data missing for ForgotPassword message");
        }

        await _telegramBot.SendChannelMessageAsync($"{FirstName} {LastName} Invoked Forgot Password");

        await _commsHandler.SendForgotPasswordEmail(Email, UserId, FirstName, LastName, ResetUrl);
    }

    private bool ValidateForgotPasswordMessage(ServiceBusCommsMessage message, out string Email, out string UserId, out string FirstName, out string LastName, out string ResetUrl)
    {
        Email = string.Empty;
        UserId = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        ResetUrl = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out Email) &&
               message.MessageData?.TryGetValue("ResetUrl", out ResetUrl) == true &&
               message.RelatedEntities.TryGetValue("UserId", out UserId) &&
               message.RelatedEntities.TryGetValue("FirstName", out FirstName) &&
               message.RelatedEntities.TryGetValue("LastName", out LastName);

    }
}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS8601 // Possible null reference assignment.
