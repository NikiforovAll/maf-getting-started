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

var historyDir = Path.Combine(Path.GetTempPath(), "maf-chat-history");
Directory.CreateDirectory(historyDir);
Console.WriteLine($"History dir: {historyDir}");

var historyProvider = new FileChatHistoryProvider(historyDir);

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
            ChatHistoryProvider = historyProvider,
        }
    );

AgentSession session = await agent.CreateSessionAsync();

await agent.RunAsync("My name is Alice and I work at Contoso.", session);
Console.WriteLine(await agent.RunAsync("What is my name and where do I work?", session));

// Simulate restart — new agent, new session, same session ID
var sessionId = historyProvider.GetSessionId(session);
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
            ChatHistoryProvider = new FileChatHistoryProvider(historyDir, sessionId),
        }
    );

AgentSession session2 = await agent2.CreateSessionAsync();
Console.WriteLine(await agent2.RunAsync("Do you remember my name?", session2));

var sessionFile = Path.Combine(historyDir, $"{sessionId}.json");
AnsiConsole.Write(
    new Panel(new JsonText(File.ReadAllText(sessionFile)))
        .Header($"Chat History (session: {sessionId})")
        .Collapse()
        .BorderColor(Color.Yellow)
);

Directory.Delete(historyDir, recursive: true);

// Session-aware provider — each session gets its own file via ProviderSessionState
sealed class FileChatHistoryProvider : ChatHistoryProvider
{
    private readonly string _directory;
    private readonly ProviderSessionState<SessionState> _sessionState;

    public FileChatHistoryProvider(string directory, string? existingSessionId = null)
    {
        _directory = directory;
        _sessionState = new ProviderSessionState<SessionState>(
            _ => new SessionState(existingSessionId ?? Guid.NewGuid().ToString("N")[..8]),
            nameof(FileChatHistoryProvider)
        );
    }

    public string GetSessionId(AgentSession? session) =>
        _sessionState.GetOrInitializeState(session).SessionId;

    private string GetFilePath(string sessionId) => Path.Combine(_directory, $"{sessionId}.json");

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default
    )
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        var path = GetFilePath(state.SessionId);

        if (!File.Exists(path))
            return new(Enumerable.Empty<ChatMessage>());

        var json = File.ReadAllText(path);
        var messages =
            JsonSerializer.Deserialize(json, ChatHistoryJsonContext.Default.ListChatMessage) ?? [];
        return new(messages.AsEnumerable());
    }

    protected override ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default
    )
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        var path = GetFilePath(state.SessionId);

        List<ChatMessage> existing = [];
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            existing =
                JsonSerializer.Deserialize(json, ChatHistoryJsonContext.Default.ListChatMessage)
                ?? [];
        }

        existing.AddRange(context.RequestMessages);
        existing.AddRange(context.ResponseMessages ?? []);

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(existing, ChatHistoryJsonContext.Default.ListChatMessage)
        );
        return default;
    }

    sealed class SessionState(string sessionId)
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; } = sessionId;
    }
}

[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
partial class ChatHistoryJsonContext : JsonSerializerContext;
