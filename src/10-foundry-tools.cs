#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:property EnablePreviewFeatures=true

using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location")] string location) =>
    $"The weather in {location} is sunny with a high of 22°C.";

[Description("Get the current time in a given timezone.")]
static string GetTime([Description("The timezone (e.g., UTC, CET)")] string timezone) =>
    $"The current time in {timezone} is {DateTime.UtcNow:HH:mm} UTC.";

const string AgentName = "FoundryToolsAgent";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

AITool[] tools = [AIFunctionFactory.Create(GetWeather), AIFunctionFactory.Create(GetTime)];

// Create agent with tools — server stores tool schemas, client provides invocable implementations
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: AgentName,
    model: deploymentName,
    instructions: "You are a helpful assistant with access to weather and time tools.",
    tools: tools
);

// Non-streaming
Console.WriteLine("--- Non-streaming ---");
var session = await agent.CreateSessionAsync();
Console.WriteLine(
    await agent.RunAsync("What's the weather in Amsterdam and what time is it in CET?", session)
);

// Streaming
Console.WriteLine("\n--- Streaming ---");
session = await agent.CreateSessionAsync();
await foreach (var update in agent.RunStreamingAsync("What's the weather in Kyiv?", session))
{
    Console.Write(update);
}
Console.WriteLine();

// Retrieve existing agent by name — must pass tools for invocation
AIAgent existing = await aiProjectClient.GetAIAgentAsync(name: AgentName, tools: tools);
Console.WriteLine("\n--- Retrieved agent ---");
Console.WriteLine(await existing.RunAsync("What time is it in UTC?"));

await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
Console.WriteLine("\nAgent deleted.");
