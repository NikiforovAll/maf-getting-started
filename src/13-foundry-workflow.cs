#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package Azure.Identity@1.18.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true

using System.ClientModel;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Spectre.Console;

const string StorytellerAgentName = "StorytellerAgent";
const string CriticAgentName = "CriticAgent";
const string WorkflowName = "StoryCriticWorkflow";

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// Step 1: Create the two Foundry agents
AnsiConsole.Write(new Rule("[bold yellow]Creating Agents[/]").LeftJustified());

await aiProjectClient.CreateAIAgentAsync(
    name: StorytellerAgentName,
    model: deploymentName,
    instructions: "You are a creative storyteller. Write a short story (3-5 sentences) based on the user's prompt. Be vivid and imaginative."
);
AnsiConsole.MarkupLine($"[green]Created:[/] {StorytellerAgentName}");

await aiProjectClient.CreateAIAgentAsync(
    name: CriticAgentName,
    model: deploymentName,
    instructions: "You are a literary critic. Review the story and provide brief constructive feedback (2-3 sentences). Highlight what works and suggest one improvement."
);
AnsiConsole.MarkupLine($"[green]Created:[/] {CriticAgentName}");

// Step 2: Register declarative workflow in Foundry via raw JSON
AnsiConsole.Write(new Rule("[bold yellow]Registering Workflow[/]").LeftJustified());

string workflowYaml = $"""
    kind: Workflow
    trigger:
      kind: OnConversationStart
      id: story_critic_workflow
      actions:
        - kind: InvokeAzureAgent
          id: storyteller_step
          conversationId: =System.ConversationId
          agent:
            name: {StorytellerAgentName}
        - kind: InvokeAzureAgent
          id: critic_step
          conversationId: =System.ConversationId
          agent:
            name: {CriticAgentName}
    """;

string escapedYaml = JsonEncodedText.Encode(workflowYaml).ToString();
string requestJson = $$"""
    {
        "definition": {
            "kind": "workflow",
            "workflow": "{{escapedYaml}}"
        },
        "description": "Storyteller writes a story, Critic reviews it."
    }
    """;

ClientResult result = await aiProjectClient.Agents.CreateAgentVersionAsync(
    WorkflowName,
    BinaryContent.Create(BinaryData.FromString(requestJson)),
    foundryFeatures: null,
    options: null
);

using var doc = JsonDocument.Parse(result.GetRawResponse().Content);
string workflowVersionStr = doc.RootElement.GetProperty("version").GetString()!;
AnsiConsole.MarkupLine($"[green]Workflow registered:[/] {WorkflowName}:{workflowVersionStr}");

// Step 3: Run the workflow with streaming
AnsiConsole.Write(new Rule("[bold yellow]Running Workflow[/]").LeftJustified());

ChatClientAgent workflowAgent = await aiProjectClient.GetAIAgentAsync(name: WorkflowName);
AgentSession session = await workflowAgent.CreateSessionAsync();

var conversationsClient = aiProjectClient.GetProjectOpenAIClient().GetProjectConversationsClient();
var conversation = await conversationsClient.CreateProjectConversationAsync();
AnsiConsole.MarkupLine($"[dim]Conversation ID:[/] {conversation.Value.Id}");

ChatClientAgentRunOptions runOptions = new(
    new ChatOptions { ConversationId = conversation.Value.Id }
);

string prompt = "Write a story about a robot who discovers music for the first time.";
AnsiConsole.MarkupLine($"[bold blue]User:[/] {prompt}");
Console.WriteLine();

string[] agentNames = [StorytellerAgentName, CriticAgentName];
int agentIndex = 0;
string? lastMessageId = null;
await foreach (var update in workflowAgent.RunStreamingAsync(prompt, session, runOptions))
{
    if (update.MessageId != lastMessageId)
    {
        Console.WriteLine();
        string name = agentIndex < agentNames.Length ? agentNames[agentIndex] : "unknown";
        AnsiConsole.MarkupLine($"\n[bold green]{name}:[/]");
        lastMessageId = update.MessageId;
        agentIndex++;
    }

    Console.Write(update.Text);
}
Console.WriteLine();

// Cleanup
AnsiConsole.Write(new Rule("[bold yellow]Cleanup[/]").LeftJustified());
if (AnsiConsole.Confirm("Delete agents and workflow?"))
{
    await aiProjectClient.Agents.DeleteAgentAsync(StorytellerAgentName);
    await aiProjectClient.Agents.DeleteAgentAsync(CriticAgentName);
    await aiProjectClient.Agents.DeleteAgentAsync(WorkflowName);
    await conversationsClient.DeleteConversationAsync(conversation.Value.Id);
    AnsiConsole.MarkupLine("[green]All resources deleted.[/]");
}
else
{
    AnsiConsole.MarkupLine("[yellow]Resources kept. Remember to clean up manually.[/]");
}
