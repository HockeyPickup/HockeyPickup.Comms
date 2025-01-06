using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HockeyPickup.Comms.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<ICommsHandler, CommsHandler>();
        services.AddSingleton<IMessageProcessor, MessageProcessor>();
        var telegramBot = new TelegramBot();
        services.TryAddSingleton(telegramBot);

    })
    .Build();

host.Run();
