using Azure.Identity;
using Azure.Messaging.EventGrid;
using Devlooped.Extensions.AI.Grok;
using Devlooped.WhatsApp;
using Gropilot;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
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

var interactiveCreds = builder.Environment.IsDevelopment();
builder.Configuration.AddAzureKeyVault(new Uri("https://gropilot.vault.azure.net/"), new DefaultAzureCredential(interactiveCreds));

builder.Services.AddKeyedChatClient("grok", new GrokClient(
    Throw.IfNullOrEmpty(builder.Configuration["XAI_API_KEY"], "XAI_API_KEY"),
    new GrokClientOptions
    {
    }).AsIChatClient("grok-4-1-fast"));

builder.Services.AddAIAgent("gropilot",
    """
    You are Gropilot, a Grok-powered AI developer copilot designed to assist with all GitHub-related tasks. 
    You can help users with code reviews, pull requests, issue management, and repository maintenance. 
    Always provide clear, concise, and actionable responses to help users effectively manage their GitHub projects.
    """,
    """
    Grok-powered developer copilot for your all your GitHub needs.
    """,
    "grok")
    .WithAITool(new HostedMcpServerTool("GitHub", "https://api.githubcopilot.com/mcp/")
    {
        AuthorizationToken = Throw.IfNullOrEmpty(builder.Configuration["GITHUB_TOKEN"], "GITHUB_TOKEN"),
    });

builder.AddOpenAIResponses();

#region WhatsApp Setup

builder.UseWhatsApp();

var whatsapp = builder.Services.AddWhatsApp<AgentHandler>()
    .UseOpenTelemetry("Gropilot")
    .UseLogging()
    .UseIgnore();

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

#endregion

var app = builder.Build();

app.Run();
