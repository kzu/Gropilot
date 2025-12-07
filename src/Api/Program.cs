using Azure.Identity;
using Azure.Messaging.EventGrid;
using Devlooped.WhatsApp;
using Gropilot;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Prevents auto-removal of using Microsoft.Azure.Functions.Worker
_ = nameof(FunctionsApplicationInsightsExtensions);

#if CI || RELEASE
builder.Environment.EnvironmentName = "Production";
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
#else
builder.Environment.EnvironmentName = "Development";
#endif

builder.UseWhatsApp();

var whatsapp = builder.Services.AddWhatsApp<AgentHandler>()
    .UseOpenTelemetry("Gropilot")
    .UseLogging()
    .UseIgnore();

var interactiveCreds = builder.Environment.IsDevelopment();
builder.Configuration.AddAzureKeyVault(new Uri("https://gropilot.vault.azure.net/"), new DefaultAzureCredential(interactiveCreds));

if (builder.Environment.IsProduction())
{
    // If event grid is set up, switch to processing messages using that
    if (builder.Configuration["EventGrid:Topic"] is { Length: > 0 } topic &&
        builder.Configuration["EventGrid:Key"] is { Length: > 0 } key)
    {
        whatsapp.UseEventGridProcessor(new EventGridPublisherClient(
            new Uri(topic), new Azure.AzureKeyCredential(key)));
    }
    // Else default is used, queue processor via azure storage
}
else
{
    // In devlopment, process inline
    whatsapp.UseTaskSchedulerProcessor();

    // Make sure we never timeout when calling back to the console
    builder.Services.AddHttpClient()
        .ConfigureHttpClientDefaults(client => client.ConfigureHttpClient(http =>
          http.Timeout = TimeSpan.FromMinutes(30)));
}


builder.Build().Run();
