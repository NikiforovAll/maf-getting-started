#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package Azure.Identity@1.18.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true

using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Spectre.Console;

const string AgentName = "FoundryPersistentSessionAgent";

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

Console.WriteLine("--- Creating Foundry Agent ---");
ChatClientAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: AgentName,
    model: deploymentName,
    instructions: "You are a friendly assistant. Keep your answers brief."
);

Console.WriteLine($"Agent created: {agent.Name}");

// Create a server-side conversation — persisted in Foundry, visible in Portal
ProjectConversationsClient conversationsClient = aiProjectClient
    .GetProjectOpenAIClient()
    .GetProjectConversationsClient();
ProjectConversation conversation = await conversationsClient.CreateProjectConversationAsync();
Console.WriteLine($"Conversation ID: {conversation.Id}");

// Session 1: establish context
AnsiConsole.Write(new Rule("[bold yellow]Session 1[/]").LeftJustified());
AgentSession session1 = await agent.CreateSessionAsync(conversation.Id);

string prompt1 = "My name is Alex and I'm building a .NET app that uses Azure AI Foundry.";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt1}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt1, session1)}");

string prompt2 = "I prefer concise answers with code examples when possible.";
AnsiConsole.MarkupLine($"\n[bold blue]User:[/] {prompt2}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt2, session1)}");

// Session 2: resume with the same conversation ID — agent remembers
AnsiConsole.Write(new Rule("[bold yellow]Session 2 (resumed)[/]").LeftJustified());
AgentSession session2 = await agent.CreateSessionAsync(conversation.Id);

string prompt3 = "What's my name, what am I building, and how do I prefer my answers?";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt3}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt3, session2)}");

// Cleanup
if (AnsiConsole.Confirm($"Delete agent [bold]{agent.Name}[/] and conversation?"))
{
    await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
    await conversationsClient.DeleteConversationAsync(conversation.Id);
    AnsiConsole.MarkupLine("[green]Agent and conversation deleted.[/]");
}
else
{
    AnsiConsole.MarkupLine("[yellow]Resources kept. Remember to clean up manually.[/]");
}
