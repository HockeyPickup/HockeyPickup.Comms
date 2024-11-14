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

    public MessageProcessor(ICommsHandler commsHandler, ILogger<MessageProcessor> logger)
    {
        _commsHandler = commsHandler;
        _logger = logger;
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

            default:
                throw new ArgumentException($"Unknown message type: {message.Metadata["Type"]}");
        }
    }

    private async Task ProcessSignedIn(ServiceBusCommsMessage message)
    {
        if (!ValidateSignedInMessage(message, out var Email))
        {
            throw new ArgumentException("Required data missing for SignedIn message");
        }

        await _commsHandler.SendSignedInEmail(Email);
    }

    private bool ValidateSignedInMessage(ServiceBusCommsMessage message, out string Email)
    {
        Email = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out Email);
    }

    private async Task ProcessRegisterConfirmation(ServiceBusCommsMessage message)
    {
        if (!ValidateRegisterConfirmationMessage(message, out var Email, out var UserId, out var FirstName, out var LastName, out var ConfirmationUrl))
        {
            throw new ArgumentException("Required data missing for RegisterConfirmation message");
        }

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
