---
marp: true
title: "Microsoft Agent Framework: Foundations"
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **Microsoft Agent Framework**
## Foundations — Your First Agents in .NET

---

<!-- _class: hero -->

![bg left:35% brightness:1.00](./img/oleksii.png)

## Oleksii Nikiforov

- Lead Software Engineer at EPAM Systems
- AI Engineering Coach
- +10 years in software development
- Open Source and Blogging

<br/>

> <i class="fa-brands fa-github"></i> [nikiforovall](https://github.com/nikiforovall)
<i class="fa-brands fa-linkedin"></i> [Oleksii Nikiforov](https://www.linkedin.com/in/nikiforov-oleksii/)
<i class="fa fa-window-maximize"></i> [nikiforovall.blog](https://nikiforovall.blog/)

---
<style scoped>
section {
  font-size: 34px;
}
</style>
![bg fit](./img/bg-alt2.png)

# Agenda

1. **What is MAF?** — The merger of Semantic Kernel + AutoGen
2. **Your First Agent** — `AzureOpenAIClient` → `.AsAIAgent()`, Run & Stream
3. **Tools** — Function tools, `[Description]`, agent-as-tool
4. **DI Hosting** — `AddAIAgent`, class-based tools from the container
5. **Agent Skills** — Portable packages of domain expertise
6. **Multi-Turn Conversations** — `AgentSession`, chat history
7. **Memory & Persistence** — Serialization, session restore

---

![bg fit](./img/bg-section.png)

# What is&nbsp;**MAF?**

## Semantic Kernel + AutoGen → One Framework

---

![bg fit](./img/bg-alt2.png)

# The Evolution

MAF unifies the best of both worlds:

| Before | After |
|--------|-------|
| **Semantic Kernel** — enterprise AI orchestration | **Microsoft.Agents.AI** — unified agent runtime |
| **AutoGen** — multi-agent research framework | Single API for single & multi-agent scenarios |
| Two ecosystems, overlapping goals | Built on **Microsoft.Extensions.AI** abstractions |

<br/>

<div class="key">

**MAF** = Microsoft Agent Framework — the production-ready successor (stable release, `1.1.0`)

</div>

---

![bg fit](./img/bg-alt3.png)

# Core Architecture

Built on **Microsoft.Extensions.AI**. It makes MAF provider-agnostic and extensible by design:

| Layer | Components |
|-------|-----------|
| **Your Application** | AIAgent, Tools, Sessions, Workflows |
| **Microsoft.Agents.AI** | Unified agent runtime |
| **Microsoft.Extensions.AI** | `IChatClient`, `AIFunction` |
| **Providers** | Azure OpenAI, OpenAI, Ollama, ... |

<br/>

<div class="tip">

**Provider-agnostic** — swap the model provider without changing agent code

</div>

---

![bg fit](./img/bg-alt1.png)

# Key Concepts



| Concept | Type | Purpose |
|---------|------|---------|
| **AIAgent** | `IAIAgent` | Core agent abstraction |
| **Tools** | `AIFunction` | Functions the agent can call |
| **Session** | `AgentSession` | Conversation state & history |
| **Run** | `RunAsync` / `RunStreamingAsync` | Execute agent with input |
| **Workflow** | `WorkflowBuilder` | Multi-agent orchestration |

---

![bg fit](./img/bg-section.png)

# Your&nbsp;**First Agent**

## From zero to running in 30 lines

---


![bg fit](./img/bg-alt3.png)

# 01-hello-agent.cs — Package Directives

```ts
#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
```

---


![bg fit](./img/bg-alt2.png)

# 01-hello-agent.cs — Creating an Agent

```ts
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = "HelloAgent",
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a friendly assistant. Keep your answers brief.",
                Temperature = 0.9f,
            },
        }
    );
```

---

![bg fit](./img/bg-alt1.png)

# 01-hello-agent.cs — Run & Stream

```ts
// Non-streaming — get the full response at once
Console.WriteLine(await agent.RunAsync("Tell me a one-sentence fun fact."));

// Streaming — process tokens as they arrive
await foreach (var update in agent.RunStreamingAsync("Tell me a one-sentence fun fact."))
{
    Console.WriteLine(update);
}
```

---


![bg fit](./img/bg-alt2.png)

# The Pipeline

`IChatClient` abstraction makes it easy to replace the underlying chat client without changing agent code.

<br/>

| Step | Call | Role |
|------|------|------|
| 1 | `AzureOpenAIClient` | Azure OpenAI provider |
| 2 | `.GetChatClient("gpt-4o-mini")` | `ChatClient` via `IChatClient` |
| 3 | `.AsAIAgent(options)` | `AIAgent` from MAF |
| 4 | `.RunAsync("prompt")` | Execute and get response |

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/01-hello-agent.cs`

---

![bg fit](./img/bg-section.png)

# **Tools**

## Giving agents the ability to act

---

![bg fit](./img/bg-alt2.png)

# Function Tools — The Pattern

```ts
// 1. Define a plain C# method with [Description] attributes
[Description("Get the weather for a given location.")]
static string GetWeather(
    [Description("The location to get the weather for.")] string location) =>
    $"The weather in {location} is cloudy with a high of 15°C.";

// 2. Register as a tool via AIFunctionFactory
AIAgent weatherAgent = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful weather assistant.",
        name: "WeatherAgent",
        tools: [AIFunctionFactory.Create(GetWeather)]
    );
```

---


![bg fit](./img/bg-alt3.png)

# How Tool Calling Works

1. **User** sends: *"What's the weather in Amsterdam?"*
2. **LLM** decides to call `GetWeather("Amsterdam")`
3. **MAF** invokes the C# method automatically
4. **Result**: *"The weather in Amsterdam is cloudy with a high of 15C."*
5. **LLM** generates final answer using the tool result

<br/>

<div class="key">

**`[Description]`** attributes are sent to the LLM as the tool schema — write clear, specific descriptions

</div>

---


![bg fit](./img/bg-alt1.png)

# Agent-as-Tool — Composing Agents

```ts
// WeatherAgent becomes a tool for the orchestrator
AIAgent orchestrator = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful assistant. "
            + "Use the weather agent when asked about weather.",
        tools: [weatherAgent.AsAIFunction()]
    );

// Orchestrator delegates to WeatherAgent when needed
Console.WriteLine(
    await orchestrator.RunAsync("What's the weather in Amsterdam and Paris?")
);
```

<div class="tip">

**`.AsAIFunction()`** wraps any `AIAgent` as a callable tool — agents composing agents

</div>

---

![bg fit](./img/bg-alt2.png)

# Tool Composition Diagram

1. **Orchestrator** receives: *"What's the weather in Amsterdam and Paris?"*
2. **Orchestrator** calls `WeatherAgent.AsAIFunction()`
   - WeatherAgent calls `GetWeather("Amsterdam")`
   - WeatherAgent calls `GetWeather("Paris")`
3. **Orchestrator** combines results into final response

<br/>

<div class="warning">

**Each agent has its own LLM call** — be mindful of latency and cost when nesting agents

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/02-tools.cs`

---

![bg fit](./img/bg-section.png)

# **DI Hosting**

## Agents and tools from the container

---

![bg fit](./img/bg-alt2.png)

# 02b-tools-di.cs — Register Agent in DI

```ts
var builder = Host.CreateApplicationBuilder(args);

IChatClient chatClient = new AzureOpenAIClient(
        new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient();
builder.Services.AddSingleton(chatClient);

builder.Services.AddSingleton<WeatherService>();

builder.AddAIAgent(
        "weather-agent",
        instructions: "You are a helpful weather assistant.",
        description: "An agent that answers weather questions.",
        chatClientServiceKey: null)
    .WithAITool(sp => sp.GetRequiredService<WeatherService>().AsAITool());
```

---

![bg fit](./img/bg-alt3.png)

# 02b-tools-di.cs — Class-Based Tool

```ts
internal sealed class WeatherService
{
    [Description("Get the weather for a given location.")]
    public string GetWeather([Description("The location")] string location) =>
        $"The weather in {location} is cloudy with a high of 15°C.";

    public AITool AsAITool() => AIFunctionFactory.Create(GetWeather);
}
```

<div class="key">

**`WithAITool(sp => ...)`** — tools resolved from DI, not static functions. Dependencies are injected normally.

</div>

---

![bg fit](./img/bg-alt1.png)

# 02b-tools-di.cs — Resolve & Run

```ts
using var host = builder.Build();

var agent = host.Services.GetRequiredKeyedService<AIAgent>("weather-agent");

Console.WriteLine(await agent.RunAsync("What's the weather in Amsterdam and Paris?"));
```

<div class="tip">

**Agents are keyed services** — resolve with `GetRequiredKeyedService<AIAgent>("name")`

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/02b-tools-di.cs`

---

![bg fit](./img/bg-section.png)

# **Agent Skills**

## Portable packages of domain expertise

---

![bg fit](./img/bg-alt2.png)

# What are Agent Skills?

Modular packages of instructions, references, and assets that agents load **on demand**.

| Stage | What Happens | Context Cost |
|-------|-------------|-------------|
| **Discover** | Names + descriptions injected into system prompt | ~100 tokens/skill |
| **Load** | Agent calls `load_skill` when task matches | < 5000 tokens |
| **Read** | Agent reads references/assets as needed | On demand |

<div class="key">

**Progressive disclosure** — agents only load what they need, keeping the context window lean

</div>

---

![bg fit](./img/bg-alt3.png)

# Skill Structure

```
skills/
└── code-review/
    ├── SKILL.md              ← Frontmatter + instructions
    └── references/
        └── STYLE_GUIDE.md    ← Reference loaded on demand
```

```yaml
---
name: code-review
description: Review code for quality, security, and best practices.
metadata:
  author: demo
  version: "1.0"
---
```

---

![bg fit](./img/bg-alt1.png)

# 02c-skills.cs — Wiring Skills

```ts
var skillsProvider = new AgentSkillsProvider(skillsDir);

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "SkillsAgent",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a helpful assistant.",
        },
        AIContextProviders = [skillsProvider],
    });
```

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/02c-skills.cs`

---

![bg fit](./img/bg-section.png)

# **Multi-Turn**&nbsp;Conversations

## Maintaining context across interactions

---

![bg fit](./img/bg-alt3.png)

# AgentSession — Conversation State

```ts
AIAgent agent = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a friendly assistant. Keep your answers brief.",
        name: "ConversationAgent"
    );

// Create a session to maintain conversation history
AgentSession session = await agent.CreateSessionAsync();
```

<div class="key">

**`AgentSession`** holds the conversation thread — pass it to every `RunAsync` call to maintain context

</div>

---

![bg fit](./img/bg-alt2.png)

# Multi-Turn in Action

```ts
// Turn 1 — introduce context
Console.WriteLine(await agent.RunAsync("My name is Alice and I love hiking.", session));

// Turn 2 — agent remembers from session history
Console.WriteLine(await agent.RunAsync("What do you remember about me?", session));

// Turn 3 — agent uses accumulated context
Console.WriteLine(await agent.RunAsync("Suggest a hiking destination for me.", session));
```

<br/>

<div class="tip">

Without a session, each `RunAsync` call is **stateless** — the agent has no memory of prior turns

</div>

---

![bg fit](./img/bg-alt1.png)

# Under the Hood

| Turn | Input | ChatHistory contents |
|------|-------|---------------------|
| *created* | — | `[]` (empty) |
| 1 | *"My name is Alice..."* | `[system, user1, assistant1]` |
| 2 | *"What do you remember?"* | `[system, user1, assistant1, user2, assistant2]` |
| 3 | *"Suggest a hiking destination"* | `[system, user1, assistant1, user2, assistant2, user3, assistant3]` |

<br/>

- Default: `InMemoryChatHistoryProvider`
- Session accumulates full conversation → sent with each LLM call
- Serialization available for persistence (next section)

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/03-multi-turn.cs`

---

![bg fit](./img/bg-section.png)

# **Memory**&nbsp;& Persistence

## Keeping agents stateful across sessions

---

![bg fit](./img/bg-alt3.png)

# The Problem

- **Session A** (in memory): User says *"My name is Alice"* — Agent remembers
- **Process restart** — memory is lost
- **Session B** (new process): User asks *"What is my name?"* — Agent has no idea

<br/>

<div class="warning">

**Default `InMemoryChatHistoryProvider`** — conversation state lives only in process memory

</div>

---


![bg fit](./img/bg-alt2.png)

# 04-memory.cs — In-Memory History

```ts
// InMemoryChatHistoryProvider is used automatically
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a friendly assistant. Keep your answers brief.",
        name: "MemoryAgent"
    );

AgentSession session = await agent.CreateSessionAsync();

await agent.RunAsync("Hello! What's the square root of 9?", session);
await agent.RunAsync("My name is Alice", session);

// Agent remembers — chat history accumulates in the session
await agent.RunAsync("What is my name?", session);
// → "Your name is Alice!"
```

---

![bg fit](./img/bg-alt1.png)

# Session Serialization

```ts
// Serialize session to JSON for persistence
var serialized = await agent.SerializeSessionAsync(session);
Console.WriteLine($"Session serialized ({serialized.GetRawText().Length} bytes)");

// Store serialized JSON anywhere — database, file, Redis, blob storage

// Restore session from serialized data
var restoredSession = await agent.DeserializeSessionAsync(serialized);

// Agent remembers everything from the original session
await agent.RunAsync("Do you still remember my name?", restoredSession);
// → "Yes, your name is Alice!"
```

<div class="key">

**`SerializeSessionAsync` / `DeserializeSessionAsync`** — portable session state for any storage backend

</div>

---

![bg fit](./img/bg-alt2.png)

# Memory Architecture

| Component | Description |
|-----------|-------------|
| **AIAgent** | Core agent abstraction |
| **AgentSession** | Holds conversation state |
| **InMemoryChatHistoryProvider** | Default, zero config, lost on restart |
| **Serialize / Deserialize** | Export to JSON blob, store anywhere |
| **Custom IChatHistoryProvider** | Implement your own for database-backed history |

<br/>

- **Short-lived**: `InMemoryChatHistoryProvider` — default, zero config
- **Persistent**: Serialize → store → deserialize on next session
- **Custom**: Implement `ChatHistoryProvider` for database-backed history

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/04-memory.cs`

---

<style scoped>
section {
  font-size: 26px;
}
</style>

![bg fit](./img/bg-alt3.png)

# Custom ChatHistoryProvider


```ts
public class FileChatHistoryProvider(string filePath) : ChatHistoryProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return new(Enumerable.Empty<ChatMessage>());
        var json = File.ReadAllText(filePath);
        return new(JsonSerializer.Deserialize<List<ChatMessage>>(json)!.AsEnumerable());
    }

    protected override ValueTask StoreChatHistoryAsync(
        InvokedContext context, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> existing = File.Exists(filePath)
            ? JsonSerializer.Deserialize<List<ChatMessage>>(File.ReadAllText(filePath)) ?? []
            : [];
        existing.AddRange(context.RequestMessages);
        existing.AddRange(context.ResponseMessages ?? []);
        File.WriteAllText(filePath, JsonSerializer.Serialize(existing));
        return default;
    }
}
```

---

![bg fit](./img/bg-alt1.png)

# Wiring a Custom Provider

```ts
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "PersistentAgent",
        ChatOptions = new ChatOptions { Instructions = "You are a friendly assistant." },
        ChatHistoryProvider = new FileChatHistoryProvider("chat-history.json"),
    });
```

<br/>

<div class="warning">

**Limitation** — all sessions share the same file. Multiple concurrent sessions will clobber each other's history.

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/04b-memory-custom.cs`

---

<style scoped>
section {
  font-size: 22px;
}
</style>

![bg fit](./img/bg-alt2.png)

# Session-Aware ChatHistoryProvider

Uses **`ProviderSessionState<TState>`** — per-session state stored in `AgentSession.StateBag`:

```ts
public class FileChatHistoryProvider : ChatHistoryProvider
{
    private readonly ProviderSessionState<SessionState> _sessionState;

    public FileChatHistoryProvider(string directory, string? existingSessionId = null)
    {
        _sessionState = new ProviderSessionState<SessionState>(_ => new SessionState(
            existingSessionId, nameof(FileChatHistoryProvider)));
    }

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        var path = Path.Combine(_directory, $"{state.SessionId}.json");
        // ... read from session-specific file
    }

    public record SessionState(string SessionId);
}
```

---

![bg fit](./img/bg-alt1.png)

# Wiring Session-Aware Provider

```ts
var historyProvider = new FileChatHistoryProvider(historyDir);

AIAgent agent = client.GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "PersistentAgent",
        ChatHistoryProvider = historyProvider,
    });

AgentSession session = await agent.CreateSessionAsync();
await agent.RunAsync("My name is Alice.", session);

// Simulate restart — extract session ID, create new provider with same ID
var sessionId = historyProvider.GetSessionId(session);
var restoredProvider = new FileChatHistoryProvider(historyDir, sessionId);

AIAgent agent2 = client.GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatHistoryProvider = restoredProvider,
    });

AgentSession session2 = await agent2.CreateSessionAsync();
await agent2.RunAsync("Do you remember my name?", session2); // → "Alice"
```

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/04c-memory-session-aware.cs`

---

![bg fit](./img/bg-alt2.png)

# Key Takeaways

1. **`AsAIAgent()`** — one extension method turns any `ChatClient` into a full agent

2. **`[Description]` + `AIFunctionFactory.Create()`** — plain C# methods become LLM-callable tools

3. **`.AsAIFunction()`** — any agent can become a tool for another agent

4. **`AgentSession`** — pass to `RunAsync` to maintain multi-turn conversation history

---

![bg fit](./img/bg-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)

---

![bg fit](./img/bg-title.png)

## **Next: Workflows,A2A, AG-UI & MCP**
### Executors, Pipelines & Agent-as-MCP-Server
