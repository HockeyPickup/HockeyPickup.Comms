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
    // Add more as needed
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly Dictionary<EmailTemplate, (string File, HashSet<string> RequiredTokens)> _templateConfig;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        _templateConfig = new Dictionary<EmailTemplate, (string, HashSet<string>)>
        {
            {
                EmailTemplate.SignedIn,
                ("signed_in.txt", new HashSet<string> { "EMAIL" })
            },
        };
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
            message.AddTo(to);
            message.SetSubject(subject);

            message.AddContent(MimeType.Html, body.Replace(Environment.NewLine, "<br />").Replace("\r", "<br />").Replace("\n", "<br />"));
            message.AddContent(MimeType.Text, body);

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
