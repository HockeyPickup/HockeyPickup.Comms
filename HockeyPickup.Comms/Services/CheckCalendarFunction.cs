using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using File = System.IO.File;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.
namespace HockeyPickup.Comms.Services;

public class CheckCalendarFunction
{
    private readonly string CalendarUrl = Environment.GetEnvironmentVariable("CalendarUrl");

    private readonly ILogger _logger;
    private readonly InMemoryLoggerProvider _loggerProvider;
    private readonly ICommsHandler _commsHandler;

    public CheckCalendarFunction(ILoggerFactory loggerFactory, ICommsHandler commsHandler)
    {
        _loggerProvider = new InMemoryLoggerProvider();
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(_loggerProvider);
            builder.AddProvider(new ForwardingLoggerProvider(loggerFactory));
        });
        _logger = factory.CreateLogger<CheckCalendarFunction>();
        _commsHandler = commsHandler;
    }

    #region interface
    [Function("CheckCalendar")]
    public async Task RunAsync([TimerTrigger("0 0 10,14 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation($"Timer trigger function executed: {timerInfo?.ScheduleStatus?.ToString()}");

        await ExecuteAsync();
    }

    [Function("CheckCalendarHttp")]
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

    #region helpers
    private static async Task<string?> GetICSFromFileSystem()
    {
        var icsFilename = "tspc.ics";

        return File.Exists(icsFilename) ? await File.ReadAllTextAsync(icsFilename) : null;
    }

    private async Task<string> GetICSFromHttp()
    {
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromMinutes(4);
        var icsContent = await client.GetStringAsync(CalendarUrl);

        return icsContent;
    }

    private string GetStorageFilePath()
    {
        var localPath = "calendar_state.txt"; // Local development path
        var azureHomePath = Environment.GetEnvironmentVariable("HOME");

        // Determine the environment
        var isAzureEnvironment = !string.IsNullOrEmpty(azureHomePath);

        if (isAzureEnvironment)
        {
            var dataDirectory = Path.Combine(azureHomePath, "site", "data");
            var storageFilePath = Path.Combine(dataDirectory, "calendar_state.txt");

            // Create the data directory if it doesn't exist
            if (!Directory.Exists(dataDirectory))
            {
                try
                {
                    Directory.CreateDirectory(dataDirectory);
                    _logger.LogInformation($"Created directory: {dataDirectory}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating directory: {ex.Message}");
                    // If we can't create the directory, fall back to the root path
                    storageFilePath = Path.Combine(azureHomePath, "site", "calendar_state.txt");
                }
            }

            return storageFilePath;
        }
        else
        {
            return localPath;
        }
    }

    private Task SendExceptionEmail(string errorMessage)
    {
        return _commsHandler.SendRawContentEmail("Hockey Pickup - Calendar Monitor Exception", errorMessage);
    }

    private static string FormatEmailBody(string content)
    {
        return content.Replace("\n", "<br>");
    }

    private Task SendNotificationEmail(string changes)
    {
        changes = FormatEmailBody(changes);
        return _commsHandler.SendRawContentEmail("Hockey Pickup - Calendar Update Detected", changes);
    }
    #endregion

    #region corelogic
    private async Task ExecuteAsync()
    {
        _logger.LogInformation($"{nameof(ExecuteAsync)} executed at: {DateTime.Now}");

        try
        {
            var storageFilePath = GetStorageFilePath();
            _logger.LogInformation($"Storage file path: {storageFilePath}");

            var icsContent = await GetICSFromFileSystem() ?? await GetICSFromHttp();

            var calendar = Calendar.Load(icsContent);
            var futureEvents = calendar.Events.Where(e => e.Start.AsUtc > DateTime.UtcNow && e.Summary.Contains("John Bryan")).OrderBy(e => e.Start.AsUtc).ToList();

            var previousState = File.Exists(storageFilePath) ? await File.ReadAllTextAsync(storageFilePath) : string.Empty;
            _logger.LogInformation("Previous state loaded successfully.");

            var newCalendar = new Calendar();
            foreach (var calendarEvent in futureEvents)
            {
                newCalendar.Events.Add(calendarEvent);
            }
            var serializer = new CalendarSerializer();
            var currentState = serializer.SerializeToString(newCalendar);

            var changes = ExtractCalendarChanges(previousState, currentState);

            await File.WriteAllTextAsync(storageFilePath, currentState);

            if (!string.IsNullOrWhiteSpace(changes))
            {
                await SendNotificationEmail(changes);
            }
            else
            {
                _logger.LogInformation("No changes detected since last update.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred: {ex.Message}");
            await SendExceptionEmail(ex.Message);
        }
    }

    private string ExtractCalendarChanges(string previousState, string currentState)
    {
        if (string.IsNullOrEmpty(previousState))
            return "Initial state loaded.";

        var previousCalendar = Calendar.Load(previousState);
        var currentCalendar = Calendar.Load(currentState);

        var now = DateTimeOffset.UtcNow;

        var previousEvents = previousCalendar.Events.Where(e => e.Start.AsUtc > now && e.Summary.Contains("John Bryan")).ToList();
        _logger.LogInformation($"{previousEvents.Count} previous events found");

        var currentEvents = currentCalendar.Events.Where(e => e.Start.AsUtc > now && e.Summary.Contains("John Bryan")).ToList();
        _logger.LogInformation($"{currentEvents.Count} current events found");

        var changes = new StringBuilder();

        var addedCount = 0;
        var changedCount = 0;
        var removedCount = 0;
        foreach (var currentEvent in currentEvents)
        {
            var matchingPreviousEvent = previousEvents.FirstOrDefault(e => EventsMatch(e, currentEvent));

            if (matchingPreviousEvent == null)
            {
                changes.AppendLine("Event Added:");
                AppendEventDetails(changes, currentEvent);
                addedCount++;
            }
            else if (!EventsAreEqual(matchingPreviousEvent, currentEvent))
            {
                changes.AppendLine("Event Changed:");
                changes.AppendLine("From:");
                AppendEventDetails(changes, matchingPreviousEvent);
                changes.AppendLine("To:");
                AppendEventDetails(changes, currentEvent);
                changedCount++;
            }

            previousEvents.Remove(matchingPreviousEvent);
        }

        foreach (var removedEvent in previousEvents)
        {
            changes.AppendLine("Event Removed:");
            AppendEventDetails(changes, removedEvent);
            removedCount++;
        }

        _logger.LogInformation($"{addedCount} added, {changedCount} changed, {removedCount} removed");

        return changes.ToString();
    }

    private static bool EventsMatch(CalendarEvent e1, CalendarEvent e2)
    {
        // Consider events to match if they have the same summary and start within 24 hours of each other
        return e1.Summary == e2.Summary && Math.Abs((e1.Start.AsUtc - e2.Start.AsUtc).TotalHours) < 24;
    }

    private static bool EventsAreEqual(CalendarEvent e1, CalendarEvent e2)
    {
        return e1.Start.AsUtc == e2.Start.AsUtc &&
                e1.End.AsUtc == e2.End.AsUtc &&
                e1.Summary == e2.Summary &&
                e1.Location == e2.Location &&
                e1.Description == e2.Description;
    }

    private static void AppendEventDetails(StringBuilder sb, CalendarEvent e)
    {
        sb.AppendLine($"Start: {e.Start.ToTimeZone("Pacific Standard Time")}");
        sb.AppendLine($"End: {e.End.ToTimeZone("Pacific Standard Time")}");
        sb.AppendLine($"Summary: {e.Summary}");
        sb.AppendLine($"Location: {e.Location}");
        sb.AppendLine($"Description: {e.Description}");
        sb.AppendLine();
    }
    #endregion
}
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8601 // Possible null reference assignment.
