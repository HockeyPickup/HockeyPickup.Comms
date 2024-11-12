using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HockeyPickup.Comms.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<ICommsHandler, CommsHandler>();
        services.AddSingleton<IMessageProcessor, MessageProcessor>();
    })
    .Build();

host.Run();
