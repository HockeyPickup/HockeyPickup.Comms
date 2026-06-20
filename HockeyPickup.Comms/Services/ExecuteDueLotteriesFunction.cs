using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

#pragma warning disable CS8601 // Possible null reference assignment.
namespace HockeyPickup.Comms.Services;

// Safety-net sweep: once daily, asks the API to run any due or stuck lottery draws. Mirrors CheckCalendarFunction
// (timer trigger + HTTP-trigger twin with in-memory log capture). Authenticates with the shared service key.
public class ExecuteDueLotteriesFunction
{
    private readonly string BaseApiUrl = Environment.GetEnvironmentVariable("BaseApiUrl");
    private readonly string LotteryServiceApiKey = Environment.GetEnvironmentVariable("LotteryServiceApiKey");

    private readonly ILogger _logger;
    private readonly InMemoryLoggerProvider _loggerProvider;
    private readonly ICommsHandler _commsHandler;

    public ExecuteDueLotteriesFunction(ILoggerFactory loggerFactory, ICommsHandler commsHandler)
    {
        _loggerProvider = new InMemoryLoggerProvider();
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(_loggerProvider);
            builder.AddProvider(new ForwardingLoggerProvider(loggerFactory));
        });
        _logger = factory.CreateLogger<ExecuteDueLotteriesFunction>();
        _commsHandler = commsHandler;
    }

    #region interface
    [Function("ExecuteDueLotteries")]
    public async Task RunAsync([TimerTrigger("0 0 9 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation($"Timer trigger function executed: {timerInfo?.ScheduleStatus?.ToString()}");

        await ExecuteAsync();
    }

    [Function("ExecuteDueLotteriesHttp")]
    public async Task<HttpResponseData> RunHttpAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("HTTP trigger function executed.");

        await ExecuteAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        var logs = _loggerProvider.GetLogs();
        await response.WriteStringAsync($"Function executed successfully.\n\nLog:\n{logs}");
        return response;
    }
    #endregion

    #region corelogic
    private async Task ExecuteAsync()
    {
        _logger.LogInformation($"{nameof(ExecuteAsync)} executed at: {DateTime.Now}");

        try
        {
            // Ignore cert errors for localhost during development, consistent with TelegramBot's HttpClient setup.
            using var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMinutes(4);

            var url = $"{BaseApiUrl}/lottery/execute-due";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Service-Key", LotteryServiceApiKey);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"execute-due returned {(int) response.StatusCode} {response.StatusCode}: {body}");

            if (!response.IsSuccessStatusCode)
            {
                await SendExceptionEmail($"execute-due returned {(int) response.StatusCode} {response.StatusCode}: {body}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred: {ex.Message}");
            await SendExceptionEmail(ex.Message);
        }
    }

    private Task SendExceptionEmail(string errorMessage)
    {
        return _commsHandler.SendRawContentEmail("Hockey Pickup - Execute Due Lotteries Exception", errorMessage);
    }
    #endregion
}
#pragma warning restore CS8601 // Possible null reference assignment.
