#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package Azure.Identity@1.18.0
#:package Spectre.Console@0.50.0
#:package OpenTelemetry@1.12.0
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.12.0
#:package OpenTelemetry.Extensions.Hosting@1.12.0
#:property EnablePreviewFeatures=true

using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console;

const string SourceName = "FoundryBasicsDemo";
const string AgentName = "FoundryBasicsAgent";

// OTEL setup — exports to Aspire dashboard when OTEL_EXPORTER_OTLP_ENDPOINT is set
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
Console.WriteLine($"OTLP endpoint: {otlpEndpoint ?? "(not set)"}");
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SourceName))
    .AddSource(SourceName)
    .AddSource("*Microsoft.Agents.AI")
    .AddOtlpExporter(o =>
    {
        if (otlpEndpoint is not null)
        {
            o.Endpoint = new Uri(otlpEndpoint);
        }

    })
    .Build();

using var activitySource = new ActivitySource(SourceName);

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// Create a server-side agent — managed by Foundry with name + version semantics
Console.WriteLine("--- Creating Foundry Agent ---");
AIAgent agent = (
    await aiProjectClient.CreateAIAgentAsync(
        name: AgentName,
        model: deploymentName,
        instructions: "You are a friendly assistant. Keep your answers brief."
    )
)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: SourceName)
    .Build();

Console.WriteLine($"Agent created: {agent.Name}");

// Retrieve latest version by name — same AIAgent API
AIAgent retrieved = (await aiProjectClient.GetAIAgentAsync(name: AgentName))
    .AsBuilder()
    .UseOpenTelemetry(sourceName: SourceName)
    .Build();
Console.WriteLine($"Retrieved by name: {retrieved.Name}");

// Parent span groups both calls — visible in Aspire dashboard
// Same trace ID appears in Foundry portal Traces tab
using var demoActivity = activitySource.StartActivity("foundry-basics-demo");
Console.WriteLine($"Trace ID: {demoActivity?.TraceId}");

// Non-streaming
Console.WriteLine("\n--- Non-streaming ---");
Console.WriteLine(await agent.RunAsync("Tell me a one-sentence fun fact about Azure."));

// Streaming
Console.WriteLine("\n--- Streaming ---");
await foreach (var update in agent.RunStreamingAsync("Tell me a one-sentence fun fact about .NET."))
{
    Console.Write(update);
}
Console.WriteLine();

demoActivity?.Dispose();

// Cleanup — deletes server-side agent and all its versions
if (AnsiConsole.Confirm($"Delete agent [bold]{agent.Name}[/]?"))
{
    await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
    AnsiConsole.MarkupLine("[green]Agent deleted.[/]");
}
else
{
    AnsiConsole.MarkupLine("[yellow]Agent kept. Remember to clean up manually.[/]");
}
