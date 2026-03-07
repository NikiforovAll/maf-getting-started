#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc2
#:package Azure.AI.Projects@1.2.0-beta.5
#:package Azure.Identity@1.18.0
#:property EnablePreviewFeatures=true

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Responses;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
var memoryStoreName =
    Environment.GetEnvironmentVariable("AZURE_AI_MEMORY_STORE_ID")
    ?? throw new InvalidOperationException("AZURE_AI_MEMORY_STORE_ID is not set.");

const string AgentName = "MemoryAgent";

// Scope isolates memories per user
string userScope = $"user_{Environment.MachineName}";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// MemorySearchTool — Foundry managed memory, persists across sessions
MemorySearchTool memorySearchTool = new(memoryStoreName, userScope) { UpdateDelay = 1 };

AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName,
    name: AgentName,
    instructions: """
    You are a helpful assistant that remembers past conversations.
    Use the memory search tool to recall information from previous interactions.
    When a user shares personal details or preferences, remember them.
    """,
    tools: [((ResponseTool)memorySearchTool).AsAITool()]
);

Console.WriteLine("--- Conversation 1: Share preferences ---");
var response1 = await agent.RunAsync(
    "My name is Alex. I'm a .NET developer and I prefer dark mode in all my tools."
);
Console.WriteLine($"Agent: {response1.Text}\n");

// Wait for memory indexing
await Task.Delay(3000);

Console.WriteLine("--- Conversation 2: Test recall ---");
var response2 = await agent.RunAsync("What do you know about me?");
Console.WriteLine($"Agent: {response2.Text}\n");

// Inspect memory search results
foreach (var message in response2.Messages)
{
    if (message.RawRepresentation is MemorySearchToolCallResponseItem memoryResult)
    {
        Console.WriteLine($"Memory search returned {memoryResult.Results.Count} result(s):");
        foreach (var result in memoryResult.Results)
        {
            Console.WriteLine($"  - [{result.MemoryItem.Scope}] {result.MemoryItem.Content}");
        }
    }
}

await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
Console.WriteLine("\nAgent deleted. Memory store persists for future sessions.");
