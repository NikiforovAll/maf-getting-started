#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc5
#:package Azure.AI.Projects@2.0.0-beta.2
#:package Azure.Identity@1.20.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true
#:property NoWarn=OPENAI001

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AzureAI;
using Spectre.Console;

const string AgentName = "FoundryPersistentSessionAgent";

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

Console.WriteLine("--- Creating Foundry Agent ---");
FoundryAgent agent = (FoundryAgent)
    await aiProjectClient.CreateAIAgentAsync(
        name: AgentName,
        model: deploymentName,
        instructions: "You are a friendly assistant. Keep your answers brief."
    );

Console.WriteLine($"Agent created: {agent.Name}");

// Create a server-side conversation session — persisted in Foundry, visible in Portal
AgentSession session1 = await agent.CreateConversationSessionAsync();

// Session 1: establish context
AnsiConsole.Write(new Rule("[bold yellow]Session 1[/]").LeftJustified());

string prompt1 = "My name is Alex and I'm building a .NET app that uses Azure AI Foundry.";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt1}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt1, session1)}");

string prompt2 = "I prefer concise answers with code examples when possible.";
AnsiConsole.MarkupLine($"\n[bold blue]User:[/] {prompt2}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt2, session1)}");

// Session 2: new conversation session — agent remembers within the same session
AnsiConsole.Write(new Rule("[bold yellow]Session 2 (new conversation)[/]").LeftJustified());
AgentSession session2 = await agent.CreateConversationSessionAsync();

string prompt3 = "Tell me a fun fact about .NET.";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt3}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt3, session2)}");

// Cleanup
if (AnsiConsole.Confirm($"Delete agent [bold]{agent.Name}[/]?"))
{
    await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
    AnsiConsole.MarkupLine("[green]Agent deleted.[/]");
}
else
{
    AnsiConsole.MarkupLine("[yellow]Agent kept. Remember to clean up manually.[/]");
}
