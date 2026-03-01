#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

// Default: InMemoryChatHistoryProvider is used automatically
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a friendly assistant. Keep your answers brief.",
        name: "MemoryAgent"
    );

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("=== Memory & Persistence Demo ===\n");

Console.WriteLine("User: Hello! What's the square root of 9?");
Console.WriteLine(
    $"Agent: {await agent.RunAsync("Hello! What's the square root of 9?", session)}\n"
);

Console.WriteLine("User: My name is Alice");
Console.WriteLine($"Agent: {await agent.RunAsync("My name is Alice", session)}\n");

// Agent remembers via in-memory chat history
Console.WriteLine("User: What is my name?");
Console.WriteLine($"Agent: {await agent.RunAsync("What is my name?", session)}\n");

// Serialize session for persistence
Console.WriteLine("=== Serializing session... ===");
var serialized = await agent.SerializeSessionAsync(session);
Console.WriteLine($"Session serialized ({serialized.GetRawText().Length} bytes)");

// Deserialize and continue
var restoredSession = await agent.DeserializeSessionAsync(serialized);
Console.WriteLine("Session restored from serialized data\n");

Console.WriteLine("User: Do you still remember my name?");
Console.WriteLine(
    $"Agent: {await agent.RunAsync("Do you still remember my name?", restoredSession)}"
);
