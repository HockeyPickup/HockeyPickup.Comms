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

            default:
                throw new ArgumentException($"Unknown message type: {message.Metadata["Type"]}");
        }
    }

    private async Task ProcessSignedIn(ServiceBusCommsMessage message)
    {
        if (!ValidateSignedInMessage(message, out var email))
        {
            throw new ArgumentException("Required data missing for SignedIn message");
        }

        await _commsHandler.SendSignedInEmail(email);
    }

    private bool ValidateSignedInMessage(ServiceBusCommsMessage message, out string email)
    {
        email = string.Empty;

        return message.CommunicationMethod.TryGetValue("Email", out email);
    }
}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS8601 // Possible null reference assignment.
