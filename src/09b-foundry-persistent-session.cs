#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc5
#:package Azure.AI.Projects@2.0.0-beta.2
#:package Azure.Identity@1.20.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true
#:property NoWarn=OPENAI001

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
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
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: AgentName,
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions = "You are a friendly assistant. Keep your answers brief.",
        }
    )
);
AIAgent foundryAgent = aiProjectClient.AsAIAgent(agentVersion);

// FoundryAgent wraps a ChatClientAgent; unwrap it to access the CreateSessionAsync(conversationId) overload.
ChatClientAgent agent = foundryAgent.GetService<ChatClientAgent>()!;
Console.WriteLine($"Agent created: {agent.Name}");

// Create a server-side conversation — Foundry stores the full history, visible in the Portal.
// The conversation ID is the only thing we need to persist on our side to resume later.
ProjectConversationsClient conversationsClient = aiProjectClient
    .GetProjectOpenAIClient()
    .GetProjectConversationsClient();
ProjectConversation conversation = await conversationsClient.CreateProjectConversationAsync();
AnsiConsole.MarkupLine($"[dim]Conversation created:[/] {conversation.Id}");

// Session 1: establish context — history lives server-side in the conversation
AgentSession session = await agent.CreateSessionAsync(conversation.Id);
AnsiConsole.Write(new Rule("[bold yellow]Session 1 — establish context[/]").LeftJustified());

string prompt1 = "My name is Alex and I'm building a .NET app that uses Azure AI Foundry.";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt1}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt1, session)}");

string prompt2 = "I prefer concise answers with code examples when possible.";
AnsiConsole.MarkupLine($"\n[bold blue]User:[/] {prompt2}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt2, session)}");

// Simulate process restart: all we kept is conversation.Id.
// New session, same conversation ID → Foundry replays the history server-side.
AgentSession resumed = await agent.CreateSessionAsync(conversation.Id);
AnsiConsole.Write(
    new Rule("[bold yellow]Session 2 — resumed from conversation ID[/]").LeftJustified()
);

string prompt3 = "What's my name and how do I prefer my answers?";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt3}");
AnsiConsole.MarkupLine($"[bold green]Agent:[/] {await agent.RunAsync(prompt3, resumed)}");

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
