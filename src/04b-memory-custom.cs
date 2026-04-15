#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Spectre.Console.Json@0.50.0

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Spectre.Console;
using Spectre.Console.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

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

AnsiConsole.Write(
    new Panel(new JsonText(File.ReadAllText(filePath)))
        .Header("Chat History")
        .Collapse()
        .BorderColor(Color.Yellow)
);

File.Delete(filePath);

// Simple flat-file provider — one file for all sessions (no session isolation)
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
        var messages =
            JsonSerializer.Deserialize(json, ChatHistoryJsonContext.Default.ListChatMessage) ?? [];
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
            existing =
                JsonSerializer.Deserialize(json, ChatHistoryJsonContext.Default.ListChatMessage)
                ?? [];
        }

        existing.AddRange(context.RequestMessages);
        existing.AddRange(context.ResponseMessages ?? []);

        File.WriteAllText(
            path: filePath,
            JsonSerializer.Serialize(existing, ChatHistoryJsonContext.Default.ListChatMessage)
        );
        return default;
    }
}

[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
partial class ChatHistoryJsonContext : JsonSerializerContext;
