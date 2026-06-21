# HockeyPickup.Comms — Repo Conventions

## Stack
.NET 10 Azure Functions, **isolated worker model**. Service Bus trigger (`ProcessCommsMessage`) + timer triggers. Email via `EmailService` with `.txt`/HTML templates; Telegram via `TelegramBot`.

## Patterns to follow
- **Message handling**: `MessageProcessor.ProcessMessageAsync` dispatches on `message.Metadata["Type"]` in a switch; unknown types throw `ArgumentException`. Each type has a private handler + a `Validate<Type>` method that extracts required `MessageData` keys via `out` params and fails gracefully (see `ValidateAddedPaymentMethod`). New message types must follow this exact shape, including the Telegram channel notification convention used by other handlers.
- **Timer functions**: follow `CheckCalendarFunction` — `[TimerTrigger("cron")]` plus an HTTP-trigger twin (`AuthorizationLevel.Function`) for manual invocation, `InMemoryLoggerProvider` log capture returned in the HTTP response, exception emails via `ICommsHandler.SendRawContentEmail`.
- **Configuration**: env vars via `Environment.GetEnvironmentVariable` (e.g., `ServiceBusConnectionString`, `%ServiceBusCommsQueueName%`, `BaseApiUrl`). New settings follow the same pattern; never hardcode.
- **Email templates**: follow existing template files and `EmailService` usage; keep subject/body conventions consistent with current notifications.
- **Outbound HTTP to the Api**: base URL from `BaseApiUrl` env var; authenticate with the service key pattern; log request/outcome.
