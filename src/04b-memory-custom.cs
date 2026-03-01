#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var filePath = Path.Combine(Path.GetTempPath(), "maf-chat-history.json");
Console.WriteLine($"History file: {filePath}");

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = "PersistentAgent",
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a friendly assistant. Keep your answers brief.",
            },
            ChatHistoryProvider = new FileChatHistoryProvider(filePath),
        }
    );

AgentSession session = await agent.CreateSessionAsync();

await agent.RunAsync("My name is Alice and I work at Contoso.", session);
Console.WriteLine(await agent.RunAsync("What is my name and where do I work?", session));

// Simulate restart — new agent, new session, same file
AIAgent agent2 = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = "PersistentAgent",
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a friendly assistant. Keep your answers brief.",
            },
            ChatHistoryProvider = new FileChatHistoryProvider(filePath),
        }
    );

AgentSession session2 = await agent2.CreateSessionAsync();
Console.WriteLine(await agent2.RunAsync("Do you remember my name?", session2));

File.Delete(filePath);

sealed class FileChatHistoryProvider(string filePath) : ChatHistoryProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (!File.Exists(filePath))
            return new(Enumerable.Empty<ChatMessage>());

        var json = File.ReadAllText(filePath);
        var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
        return new(messages.AsEnumerable());
    }

    protected override ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default
    )
    {
        List<ChatMessage> existing = [];
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            existing = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
        }

        existing.AddRange(context.RequestMessages);
        existing.AddRange(context.ResponseMessages ?? []);

        File.WriteAllText(filePath, JsonSerializer.Serialize(existing));
        return default;
    }
}
