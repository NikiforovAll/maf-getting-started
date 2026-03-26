---
marp: true
title: "Microsoft Agent Framework: Zero to Hero"
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **Microsoft Agent Framework**
## Zero to Hero — Agents in .NET

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
  font-size: 30px;
}
</style>

![bg fit](./img/bg-alt2.png)

# Agenda — Part I: Foundations

1. **What is MAF?** — Semantic Kernel + AutoGen → One Framework
2. **Your First Agent** — `AsAIAgent()`, Run & Stream
3. **Tools** — Function tools, `[Description]`, agent-as-tool
4. **DI Hosting** — `AddAIAgent`, class-based tools from the container
5. **Agent Skills** — Portable packages of domain expertise
6. **Multi-Turn Conversations** — `AgentSession`, chat history
7. **Memory** — In-memory state

---

<style scoped>
section {
  font-size: 30px;
}
</style>

![bg fit](./img/bg-alt3.png)

# Agenda — Part II & III

### Part II — Workflows & Protocols
8. **Workflows** — Function, Agent, and Composed workflows
9. **MCP** — Agent-as-MCP-server, agent-as-MCP-client
10. **A2A** — Agent-to-agent communication over HTTP
11. **AG-UI** — Expose agents to web UIs via HTTP + SSE

### Part III — Azure AI Foundry
12. **Foundry Agents** — Server-side managed agents with versioning & observability
13. **Hosted Tools** — Code Interpreter, Web Search, RAG
14. **Foundry Workflows & Evaluations** — Declarative orchestration, quality & safety scoring

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Part I

## Foundations

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

**MAF** = Microsoft Agent Framework — the production-ready successor (public preview, `1.0.0-rc4`)

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

# Package Directives

```ts
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc4
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
```

---


![bg fit](./img/bg-alt2.png)

# Creating an Agent

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

# Run & Stream

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

# Register Agent in DI

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

# Class-Based Tool

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

# Resolve & Run

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

# Wiring Skills

```ts
var skillsProvider = new FileAgentSkillsProvider(skillPath: skillsDir);

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

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/03-multi-turn.cs`

---

![bg fit](./img/bg-section.png)

# **Memory**

## In-memory state for agent conversations

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

# In-Memory History

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

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/04-memory.cs`

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Part II

## Workflows & Protocols

---

![bg fit](./img/bg-section.png)

# **Workflows**

## Orchestrating agents and functions as graphs

---

![bg fit](./img/bg-alt2.png)

# Three Workflow Patterns

| Pattern | Use Case | API |
|---------|----------|-----|
| **Function Workflow** | Deterministic execution | `BindAsExecutor()` + `WorkflowBuilder` |
| **Agent Workflow** | LLM-powered multi-agent pipelines | `AgentWorkflowBuilder.BuildSequential()` |
| **Composed Workflow** | Mix functions + agents in one graph | `WorkflowBuilder` + `AddEdge()` |

<br/>

<div class="key">

Workflows are **directed graphs** — nodes are executors (functions or agents), edges define data flow

</div>

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Workflow Building Blocks

| Building Block | API | Description |
| -------------- | --- | ----------- |
| **Executor** | `Func<T,R>.BindAsExecutor()` | A node in the workflow graph |
| **Direct Edge** | `builder.AddEdge(A, B)` | One-to-one — A's output flows to B |
| **Conditional Edge** | `AddEdge<T>(A, B, condition)` | Fires only when predicate is true |
| **Fan-Out** | `AddFanOutEdge(A, [B, C])` | Broadcast to parallel targets |
| **Fan-In Barrier** | `AddFanInBarrierEdge([A, B], C)` | Wait for **all** sources, then deliver |
| **Output** | `builder.WithOutputFrom(B)` | Designates the final output node(s) |
| **Run** | `InProcessExecution.RunAsync()` | Executes the workflow |

---


![bg fit](./img/bg-alt3.png)

# Function Workflow — Programmatic

```ts
// Bind plain functions as workflow executors
Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

Func<string, string> reverseFunc = s => string.Concat(s.Reverse());
var reverse = reverseFunc.BindAsExecutor("ReverseTextExecutor");

// Build graph: uppercase → reverse
WorkflowBuilder builder = new(uppercase);
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);

// Compile the workflow
var workflow = builder.Build();
```

---

![bg fit](./img/bg-alt1.png)

# Function Workflow — Execution

| Step | Executor | Output |
|------|----------|--------|
| Input | — | `"Hello, World!"` |
| 1 | UppercaseExecutor | `"HELLO, WORLD!"` |
| 2 | ReverseTextExecutor | `"!DLROW ,OLLEH"` |

```ts
await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");

foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine(
            $"{executorComplete.ExecutorId}: {executorComplete.Data}");
    }
}
```


---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/05a-workflows.cs`

---


![bg fit](./img/bg-alt2.png)

# Agent Workflow — Sequential Pipeline

```ts
var chatClient = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient();

AIAgent writer = chatClient.AsAIAgent(
    instructions: "You write short creative stories in 2-3 sentences.",
    name: "Writer");

AIAgent critic = chatClient.AsAIAgent(
    instructions: "You review stories and give brief constructive feedback in 1-2 sentences.",
    name: "Critic");

var agentWorkflow = AgentWorkflowBuilder.BuildSequential("story-pipeline", [writer, critic]);
```

---

![bg fit](./img/bg-alt3.png)

# Agent Workflow — Streaming Execution

```ts
List<ChatMessage> input = [new(ChatRole.User, "Write a story about a robot learning to paint.")];

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(agentWorkflow, input);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e)
        Console.Write(e.Update.Text);
}
```

```
[Writer]: "A small robot named Pixel discovered an abandoned art studio..."
[Critic]: "The story has a charming premise. Consider adding sensory details..."
```

<br/>

<div class="tip">

Agent workflows use **`StreamingRun`** + **`AgentResponseUpdateEvent`** — not `Run`/`NewEvents`

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/05b-workflows-agents.cs`

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Composed Workflow — Why?

**Real pipelines mix deterministic + intelligent steps**

| Step | What | Why not just one? |
|------|------|--------------------|
| **Regex** (function) | Mask emails — fast, 100% reliable | LLMs hallucinate, regex doesn't |
| **LLM** (agent) | Rewrite text naturally | Regex can't rephrase prose |
| **Regex** (function) | Validate no PII leaked | Trust but verify — deterministic check |

<br/>

<div class="key">

**`WorkflowBuilder`** lets you compose both in a single directed graph — each node is the right tool for the job

</div>

---


![bg fit](./img/bg-alt3.png)

# Composed Workflow — Code

```ts
// Function executor — mask emails with regex
var maskExecutor = maskEmails.BindAsExecutor("MaskEmails");

// Adapter — bridge string → ChatMessage + TurnToken for agent
var toAgentExecutor = new FunctionExecutor<string>("ToAgent", async (text, ctx, ct) => {
    await ctx.SendMessageAsync(new ChatMessage(ChatRole.User, text), ct);
    await ctx.SendMessageAsync(new TurnToken(emitEvents: true), ct);
}, sentMessageTypes: [typeof(ChatMessage), typeof(TurnToken)]).BindExecutor();

// Agent executor — bind directly, disable forwarding incoming messages
var rewriteExecutor = rewriter.BindAsExecutor(
    new AIAgentHostOptions { ForwardIncomingMessages = false });

// Adapter — extract string from agent's ChatMessage output
Func<List<ChatMessage>, string> fromAgent = msgs => string.Join("", msgs.Select(m => m.Text));
var fromAgentExecutor = fromAgent.BindAsExecutor("FromAgent");
```

---

![bg fit](./img/bg-alt1.png)

# Composed Workflow — Graph

```ts
// Build graph: mask → toAgent → rewrite → fromAgent → validate
WorkflowBuilder builder = new(maskExecutor);
builder.AddEdge(maskExecutor, toAgentExecutor);
builder.AddEdge(toAgentExecutor, rewriteExecutor);
builder.AddEdge(rewriteExecutor, fromAgentExecutor);
builder.WithOutputFrom(fromAgentExecutor);
var workflow = builder.Build();
```

| Step | Executor | Type | Output |
|------|----------|------|--------|
| 1 | MaskEmails | `Func` | `"contact [EMAIL_REDACTED] for..."` |
| 2 | ToAgent | Adapter | string → ChatMessage + TurnToken |
| 3 | Rewriter | `AIAgent` | Naturally rewritten text |
| 4 | FromAgent | Adapter | List\<ChatMessage\> → string |

---

![bg fit](./img/bg-alt2.png)

# Composed Workflow — Execution

```ts
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e)
        Console.Write(e.Update.Text);         // stream agent tokens
    else if (evt is ExecutorCompletedEvent c)
        Console.WriteLine($"[{c.ExecutorId}]: {c.Data}");
}
```

```
[MaskEmails]: "Hi team, contact Alice at [EMAIL_REDACTED] for the Q3 report..."
[Rewriter]: "Hi team, please reach out to Alice at [EMAIL_REDACTED] for..."
[FromAgent]: "Hi team, please reach out to Alice at [EMAIL_REDACTED] for..."
```

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/05c-workflows-composed.cs`

---

![bg fit](./img/bg-section.png)

# **MCP**&nbsp;Integration

## Agents as Model Context Protocol servers

---

![bg fit](./img/bg-alt2.png)

# What is MCP?

**Model Context Protocol** — open standard for connecting AI models to external tools and data

| Side | Component |
|------|-----------|
| **Client** | Claude, VS Code, any MCP client |
| **Server** | Your .NET agents exposed as MCP tools |

<br/>

<div class="key">

Expose your MAF agents as **MCP tools** — any MCP-compatible client can call them

</div>

---

<style scoped>
section {
  font-size: 26px;
}
</style>

![bg fit](./img/bg-alt3.png)

# Server Setup

```ts
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc4
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:package Microsoft.Extensions.Hosting@10.0.0
#:package ModelContextProtocol@1.0.0

// Create agents
AIAgent joker = client.GetChatClient(deploymentName)
    .AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker",
        description: "An agent that tells jokes on any topic.");

AIAgent weatherAgent = client.GetChatClient(deploymentName)
    .AsAIAgent(instructions: "You are a helpful weather assistant.",
        name: "WeatherAgent",
        description: "An agent that answers weather questions.",
        tools: [AIFunctionFactory.Create(GetWeather)]);
```

---

![bg fit](./img/bg-alt2.png)

# Exposing as MCP Tools

```ts
// Wrap agents as MCP tools
var jokerTool = McpServerTool.Create(joker.AsAIFunction());
var weatherTool = McpServerTool.Create(weatherAgent.AsAIFunction());

// Build and run MCP server over stdio
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools([jokerTool, weatherTool]);

await builder.Build().RunAsync();
```

<br/>

<div class="tip">

**`.AsAIFunction()` → `McpServerTool.Create()`** — two calls to go from agent to MCP tool

</div>

---

<style scoped>
section {font-size: 26px;}
</style>

![bg fit](./img/bg-alt1.png)

# .mcp.json — Client Configuration

```json
{
  "mcpServers": {
    "maf-agents": {
      "command": "dotnet",
      "args": ["run", "src/06-agent-as-mcp.cs"],
      "env": {
        "AZURE_OPENAI_ENDPOINT": "https://your-resource...azure.com/",
        "AZURE_OPENAI_DEPLOYMENT_NAME": "gpt-4o-mini"
      }
    }
  }
}
```

<br/>

- Drop this in your repo root → Claude Code / VS Code picks it up
- `dotnet run` starts the MCP server as a child process
- Communication over **stdio** — no ports, no networking

---

![bg fit](./img/bg-alt3.png)

# The Full Picture

1. **Claude Code** receives: *"What's the weather in Amsterdam?"*
2. **Discovers** available MCP tools: `Joker`, `WeatherAgent`
3. **Calls** `WeatherAgent` via stdio transport
4. **MCP Server** (`06-agent-as-mcp`) routes to `WeatherAgent` which calls `GetWeather()`
5. **Result** flows back through stdio to Claude Code

---


![bg fit](./img/bg-alt2.png)

# Agent as MCP Client

```ts
// Connect to a remote MCP server over HTTP (Streamable HTTP)
await using var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new()
    {
        Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
        Name = "Microsoft Learn MCP"
    }));

IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();

AIAgent agent = client.GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(
        instructions: "You answer questions using Microsoft Learn docs.",
        name: "DocsAgent",
        tools: [.. mcpTools.Cast<AITool>()]);
```

<div class="key">

**`HttpClientTransport`** for remote servers, **`StdioClientTransport`** for local — same `ListToolsAsync()` API

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `claude --mcp-config ./.mcp_demo.json`
## `dotnet run src/06b-agent-as-mcp-client.cs`

---

![bg fit](./img/bg-section.png)

# **A2A**&nbsp;Protocol

## Agent-to-agent communication over HTTP

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# What is A2A?

**Agent-to-Agent Protocol** — open standard for agents to discover and communicate with each other

| | MCP | A2A |
|--|-----|-----|
| **Who talks** | Client → Tool | Agent → Agent |
| **Transport** | stdio / HTTP | HTTP + JSON-RPC |
| **Discovery** | Config file | `/.well-known/agent-card.json` |
| **Use case** | Extend agent capabilities | Multi-agent orchestration |

<br/>

<div class="key">

MCP = **tools for one agent**, A2A = **agents talking to agents**

</div>

---

![bg fit](./img/bg-alt3.png)

# A2A — Agent Card

Every A2A agent publishes a card at `/.well-known/agent-card.json`

```ts
AgentCard agentCard = new()
{
    Name = "A2AAssistant",
    Description = "A helpful assistant exposed via A2A protocol.",
    Version = "1.0.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Capabilities = new() { Streaming = false },
    Skills = [new() {
        Id = "general", Name = "General Assistant",
        Description = "Answers general questions and checks weather."
    }],
};
```

<div class="tip">

Clients discover agents by fetching the card — no config files, no registry needed

</div>

---

<style scoped>
section {
  font-size: 26px;
}
</style>

![bg fit](./img/bg-alt1.png)

# A2A Server

```ts
#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Agents.AI.Hosting.A2A.AspNetCore@1.0.0-preview.260225.1
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc4

AIAgent agent = client.GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(instructions: "You are a helpful assistant.",
        name: "A2AAssistant",
        tools: [AIFunctionFactory.Create(GetWeather)]);

app.MapA2A(agent, path: "/", agentCard: agentCard);

await app.RunAsync();
```

---

![bg fit](./img/bg-alt2.png)

# A2A Client

```ts
#:package Microsoft.Agents.AI.A2A@1.0.0-preview.260225.1

A2ACardResolver resolver = new(new Uri("http://localhost:5000"));

AIAgent agent = await resolver.GetAIAgentAsync();

Console.WriteLine(await agent.RunAsync("What is the weather in Amsterdam?"));
```

<div class="key">

**6 lines** to discover a remote agent and call it — the same `AIAgent` interface everywhere

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/07a-agent-as-a2a-server.cs`
## `dotnet run src/07b-agent-as-a2a-client.cs`

---

![bg fit](./img/bg-section.png)

# **AG-UI**&nbsp;Protocol

## Exposing agents to web UIs via HTTP + SSE

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# What is AG-UI?

**Agent User Interface Protocol** — open standard for connecting agents to frontend UIs

| | MCP | A2A | AG-UI |
|--|-----|-----|-------|
| **Who talks** | Client → Tool | Agent → Agent | User → Agent |
| **Transport** | stdio / HTTP | HTTP + JSON-RPC | HTTP POST + SSE |
| **Discovery** | Config file | Agent card | URL |
| **Use case** | Extend capabilities | Multi-agent orchestration | Serve end users |

<br/>

<div class="key">

MCP = **tools**, A2A = **agents**, AG-UI = **end users**

</div>

---

<style scoped>
th,td {
  font-size: 22px;
}
</style>

![bg fit](./img/bg-alt3.png)

# How AG-UI Works

**Client sends one HTTP POST → Server streams back SSE events**

| Phase | What happens | SSE Events |
|-------|-------------|------------|
| **Start** | Server begins processing | `RUN_STARTED` |
| **Text response** | Tokens stream to UI in real-time | `TEXT_MESSAGE_START` → `TEXT_MESSAGE_CONTENT`* → `TEXT_MESSAGE_END` |
| **Tool call** | Agent invokes a function | `TOOL_CALL_START` → `TOOL_CALL_ARGS` → `TOOL_CALL_END` |
| **State update** | Shared state syncs to client | `STATE_SNAPSHOT` or `STATE_DELTA` |
| **Finish** | Run completes | `RUN_FINISHED` |

<br/>

<div class="key">

One request, one stream — the full agent turn (text, tool calls, state) delivered as typed events over SSE

</div>

---

<style scoped>
section {
  font-size: 26px;
}
</style>

![bg fit](./img/bg-alt1.png)

# AG-UI Server

```ts
#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore@1.0.0-preview.260225.1
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc4

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAGUI();              // register AG-UI JSON serialization

WebApplication app = builder.Build();

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());
AIAgent agent = client.GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(
        name: "AGUIAssistant",
        instructions: "You are a helpful assistant.",
        tools: [AIFunctionFactory.Create(GetWeather)]);

app.MapAGUI("/", agent);                // expose agent via AG-UI protocol
await app.RunAsync();
```

<div class="tip">

**`AddAGUI()`** + **`MapAGUI("/", agent)`** — two calls from agent to HTTP+SSE endpoint

</div>

---

<style scoped>
section {
  font-size: 26px;
}
</style>

![bg fit](./img/bg-alt2.png)

# AG-UI Client

```ts
#:package Microsoft.Agents.AI.AGUI@1.0.0-preview.260225.1

using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
AGUIChatClient chatClient = new(httpClient, "http://localhost:5000");

AIAgent agent = chatClient.AsAIAgent(name: "agui-client");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(
    [new ChatMessage(ChatRole.User, message)]))
{
    foreach (AIContent content in update.Contents)
        if (content is TextContent text)
            Console.Write(text.Text);
}
```

<div class="tip">

**`AGUIChatClient`** — connect to any AG-UI server; build rich UIs with **[CopilotKit](https://docs.copilotkit.ai/microsoft-agent-framework)** or **[AG-UI Dojo](https://dojo.ag-ui.com/microsoft-agent-framework-dotnet)**

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/08a-agent-as-agui.cs`
## `dotnet run src/08b-agent-as-agui-client.cs`

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Part III

## Azure AI Foundry

---

![bg fit](./img/bg-section.png)

# **Why Foundry?**

## From Azure OpenAI to managed agents

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Azure OpenAI vs MAF + Azure AI Foundry

| | MAF | Azure AI Foundry |
|--|--|--|
| **Agent lifecycle** | In-process only | Server-side (named + versioned) |
| **Tools** | Client-side `AIFunction` | Client-side + **hosted** (Code, Search, Web) |
| **Memory** | `InMemoryChatHistoryProvider` | Managed **Memory Stores** |
| **RAG** | Build your own | Hosted **vector stores** + `HostedFileSearchTool` |
| **Evaluation** | N/A | Built-in **quality + safety** evaluators |

<br/>

<div class="key">

Same **`AIAgent`** / **`RunAsync()`** API surface — the abstraction doesn't change

</div>

---

![bg fit](./img/bg-alt3.png)

# What Changes?

**New package:**
```
#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@1.2.0-beta.5
```

**New entry point:**
```ts
AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());
```

**New env var:**
```bash
export AZURE_AI_PROJECT_ENDPOINT="https://your-project.services.ai.azure.com/api"
```

<div class="tip">

`AzureOpenAIClient` → `AIProjectClient` — everything else stays the same

</div>

---

![bg fit](./img/bg-section.png)

# **First Foundry Agent**

## Server-side managed agents with versioning

---

![bg fit](./img/bg-alt2.png)

# Creating a Foundry Agent

```ts
AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// Create a server-side agent — Foundry manages it with name + version
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: "FoundryBasicsAgent",
    model: deploymentName,
    instructions: "You are a friendly assistant. Keep your answers brief.");
```

<div class="key">

Agents are **server-side resources** — named, versioned, persisted in Foundry

</div>

---

![bg fit](./img/bg-alt3.png)

# Retrieve & Run — Same API

```ts
// Retrieve latest version by name
AIAgent retrieved = await aiProjectClient.GetAIAgentAsync(name: "FoundryBasicsAgent");

// Non-streaming — same as Azure OpenAI agents
Console.WriteLine(await agent.RunAsync("Tell me a fun fact about Azure."));

// Streaming — same as Azure OpenAI agents
await foreach (var update in agent.RunStreamingAsync("Tell me a fun fact about .NET."))
{
    Console.Write(update);
}

// Cleanup — deletes server-side agent and all its versions
await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
```

---

![bg fit](./img/bg-alt1.png)

# Agent Versioning

```ts
// Each CreateAIAgentAsync call creates a new version
AIAgent v1 = await aiProjectClient.CreateAIAgentAsync(
    name: "MyAgent", model: "gpt-4o-mini",
    instructions: "You are helpful.");

AIAgent v2 = await aiProjectClient.CreateAIAgentAsync(
    name: "MyAgent", model: "gpt-4o-mini",
    instructions: "You are extremely helpful and concise.");

// GetAIAgentAsync returns the latest version
AIAgent latest = await aiProjectClient.GetAIAgentAsync(name: "MyAgent");
```

<br/>

<div class="tip">

Agent definitions are **immutable** after creation — create a new version to change instructions or tools

</div>

---

![bg fit](./img/bg-section.png)

# **Observability**

## OpenTelemetry + Foundry Traces

---

![bg fit](./img/bg-alt2.png)

# Two Sides of the Trace

|           | OTEL                                  | Server-side (Foundry)            |
| --------- | ------------------------------------- | -------------------------------- |
| **What**  | Agent spans, chat calls, duration     | Token counts, cost, response IDs |
| **Where** | Aspire Dashboard / any OTLP backend   | Foundry Portal → Traces tab      |
| **How**   | `.UseOpenTelemetry()` + OTLP exporter | Automatic — built into Foundry   |

<br/>

<div class="key">

Same **Trace ID** links client spans ↔ server traces — full end-to-end visibility

</div>

---

![bg fit](./img/bg-alt3.png)

# Adding OpenTelemetry to a Foundry Agent

```ts
// 1. Setup OTLP exporter → Aspire dashboard
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoundryBasicsDemo"))
    .AddSource("FoundryBasicsDemo")
    .AddSource("*Microsoft.Agents.AI")
    .AddOtlpExporter()
    .Build();

// 2. Wrap agent with telemetry
AIAgent agent = (await aiProjectClient.CreateAIAgentAsync(
    name: "FoundryBasicsAgent", model: deploymentName,
    instructions: "You are a friendly assistant."))
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "FoundryBasicsDemo")
    .Build();
```

---

![bg fit](./img/bg-alt1.png)

# Correlated Traces

```ts
// Parent span groups related calls
using var activitySource = new ActivitySource("FoundryBasicsDemo");
using var activity = activitySource.StartActivity("foundry-basics-demo");

Console.WriteLine($"Trace ID: {activity?.TraceId}");

await agent.RunAsync("Tell me a fun fact about Azure.");
await agent.RunStreamingAsync("Tell me a fun fact about .NET.");
```

<br/>

<div class="tip">

Print the **Trace ID** → find the same trace in Foundry Portal with token counts and cost

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/09-foundry-basics.cs`

---

![bg fit](./img/bg-section.png)

# **Persistent Sessions**

## Server-side conversations with Foundry

---

![bg fit](./img/bg-alt2.png)

# Creating a Foundry Conversation

```ts
// Create a server-side conversation — persisted in Foundry, visible in Portal
ProjectConversationsClient conversationsClient = aiProjectClient
    .GetProjectOpenAIClient()
    .GetProjectConversationsClient();

ProjectConversation conversation = await conversationsClient.CreateProjectConversationAsync();

// Link session to the conversation — history stored server-side
AgentSession session = await agent.CreateSessionAsync(conversation.Id);
Console.WriteLine(await agent.RunAsync("My name is Alex.", session));
```

<br/>

<div class="key">

`conversation.Id` is the only thing you need to store — Foundry keeps the full thread server-side

</div>

---

![bg fit](./img/bg-alt3.png)

# Resuming a Conversation

```ts
// New session, same conversation ID — agent remembers everything
AgentSession resumed = await agent.CreateSessionAsync(conversation.Id);
Console.WriteLine(await agent.RunAsync("What's my name?", resumed));
// → "Your name is Alex."
```

<br/>

<div class="tip">

Conversations **persist beyond sessions** — store the ID in your DB, resume from any process

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/09b-foundry-persistent-session.cs`

---

![bg fit](./img/bg-section.png)

# **Function Tools**

## Same pattern, managed by Foundry

---

![bg fit](./img/bg-alt2.png)

# Function Tools on Foundry

```ts
[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location")] string location)
    => $"The weather in {location} is sunny with a high of 22°C.";

AITool[] tools = [AIFunctionFactory.Create(GetWeather)];

// Server stores tool schemas, client provides invocable implementations
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: "WeatherAgent", model: deploymentName,
    instructions: "You are a helpful assistant with weather tools.",
    tools: tools);
```

---

![bg fit](./img/bg-alt3.png)

# Retrieving Agents with Tools

```ts
// IMPORTANT: Server only stores the tool schema (JSON Schema)
// You must provide invocable tools when retrieving
var existing = await aiProjectClient.GetAIAgentAsync(
    name: "WeatherAgent",
    tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await existing.RunAsync("What's the weather in Kyiv?"));
```

<div class="key">

Server stores **schemas**, client provides **implementations** — pass tools on `GetAIAgentAsync()` for automatic invocation

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/10-foundry-tools.cs`

---

![bg fit](./img/bg-section.png)

# **Hosted Tools**

## Code Interpreter, Web Search — zero infrastructure

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Hosted vs Client Tools

| | Client Tools (Sessions 1–2) | Hosted Tools (Foundry) |
|--|--|--|
| **Execution** | Your process | Foundry cloud sandbox |
| **Setup** | Define + implement | One-liner — Foundry provides runtime |
| **Examples** | `AIFunctionFactory.Create(...)` | `HostedCodeInterpreterTool`, `HostedWebSearchTool`, `HostedFileSearchTool` |
| **Use case** | Custom business logic | Python execution, web search, file search |

<br/>

<div class="key">

Hosted tools run **server-side** — no local dependencies, no infrastructure to manage

</div>

---

![bg fit](./img/bg-alt3.png)

# Code Interpreter — Python Sandbox

```ts
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName,
    name: "MathTutor",
    instructions: "You are a math tutor. Write and run Python code to solve problems.",
    tools: [new HostedCodeInterpreterTool() { Inputs = [] }]);

AgentResponse response = await agent.RunAsync(
    "Solve x^3 - 6x^2 + 11x - 6 = 0. Plot the function and mark the roots.");
```

<br/>

<div class="tip">

`HostedCodeInterpreterTool` — the agent writes Python, Foundry runs it in a sandbox, returns results + files

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/11a-foundry-code-interpreter.cs`

---

![bg fit](./img/bg-alt2.png)

# Web Search — Real-Time Queries

```ts
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: "WebSearchAgent",
    model: deploymentName,
    instructions: "Search the web to answer questions accurately. Cite your sources.",
    tools: [new HostedWebSearchTool()]);

AgentResponse response = await agent.RunAsync("What are the latest features in .NET 10?");
```

---

![bg fit](./img/bg-alt3.png)

# Extracting URL Citations

```ts
foreach (var annotation in response.Messages
    .SelectMany(m => m.Contents)
    .SelectMany(c => c.Annotations ?? []))
{
    if (annotation.RawRepresentation is UriCitationMessageAnnotation urlCitation)
    {
        Console.WriteLine($"  - {urlCitation.Title}: {urlCitation.Uri}");
    }
}
```

<br/>

<div class="key">

`HostedWebSearchTool` — agent searches the web autonomously and returns **annotated citations**

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/11b-foundry-web-search.cs`

---

![bg fit](./img/bg-section.png)

# **RAG via Foundry**

## File upload → Vector store → Grounded Q&A

---

![bg fit](./img/bg-alt2.png)

# The RAG Pipeline

| Step | API | What happens |
|------|-----|-------------|
| 1. **Upload file** | `filesClient.UploadFile()` | File stored in Foundry |
| 2. **Create vector store** | `vectorStoresClient.CreateVectorStoreAsync()` | Auto-chunked + embedded |
| 3. **Create agent** | `CreateAIAgentAsync(tools: [HostedFileSearchTool])` | Agent grounded on your data |
| 4. **Ask questions** | `agent.RunAsync()` | Grounded answers with citations |
| 5. **Cleanup** | Delete agent, vector store, file | No orphan resources |

<div class="key">

RAG in **~10 lines** — no embedding pipeline, no vector DB to manage

</div>

---

<style scoped>
section {font-size: 26px;}
</style>

![bg fit](./img/bg-alt3.png)

# Upload & Create Vector Store

```ts
var projectOpenAIClient = aiProjectClient.GetProjectOpenAIClient();
var filesClient = projectOpenAIClient.GetProjectFilesClient();
var vectorStoresClient = projectOpenAIClient.GetProjectVectorStoresClient();

// Upload knowledge base
OpenAIFile uploaded = filesClient.UploadFile(tempFile, FileUploadPurpose.Assistants);

// Create vector store — auto-chunks and embeds
var vectorStore = await vectorStoresClient.CreateVectorStoreAsync(
    new() { FileIds = { uploaded.Id }, Name = "contoso-products" });
```

---

![bg fit](./img/bg-alt1.png)

# Create Agent with File Search

```ts
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName,
    name: "RAGAgent",
    instructions: "Answer questions using the product catalog. Cite the source.",
    tools: [new HostedFileSearchTool() {
        Inputs = [new HostedVectorStoreContent(vectorStoreId)]
    }]);

// Multi-turn Q&A with grounded answers
var session = await agent.CreateSessionAsync();
Console.WriteLine(await agent.RunAsync("What's the cheapest product?", session));
Console.WriteLine(await agent.RunAsync("Which product supports CI/CD?", session));
```

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/12-foundry-rag.cs`

---

![bg fit](./img/bg-section.png)

# **Foundry Workflows**

## Declarative multi-agent orchestration

---

![bg fit](./img/bg-alt2.png)

# Workflow YAML — Storyteller + Critic

```yaml
kind: Workflow
trigger:
  kind: OnConversationStart
  id: story_critic_workflow
  actions:
    - kind: InvokeAzureAgent
      id: storyteller_step
      conversationId: =System.ConversationId
      agent:
        name: StorytellerAgent
    - kind: InvokeAzureAgent
      id: critic_step
      conversationId: =System.ConversationId
      agent:
        name: CriticAgent
```

<div class="key">

Declarative YAML defines the agent graph — **registered server-side**, visible in Foundry Portal's Workflows tab

</div>

---

<style scoped>
section {font-size: 26px;}
</style>

![bg fit](./img/bg-alt3.png)

# Registering & Running a Workflow

```ts
// Register workflow in Foundry
AgentVersion workflowVersion = await aiProjectClient.Agents
    .CreateAgentVersionAsync(WorkflowName, new(workflowDefinition));

// Run with streaming — each agent produces a separate message
ChatClientAgent workflowAgent = await aiProjectClient.GetAIAgentAsync(name: WorkflowName);

ChatClientAgentRunOptions runOptions = new(
    new ChatOptions { ConversationId = conversation.Id });

await foreach (var update in workflowAgent.RunStreamingAsync(prompt, session, runOptions))
{
    Console.Write(update.Text);
}
```

<div class="tip">

Same `RunStreamingAsync` API — Foundry orchestrates the agents server-side, streams results back

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/13-foundry-workflow.cs`

---

![bg fit](./img/bg-section.png)

# **Evaluations**

## Quality and safety scoring

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Available Evaluators

| Dimension | Evaluator | What it measures |
|-----------|-----------|-----------------|
| **Groundedness** | `GroundednessEvaluator` | Are answers grounded in provided context? |
| **Relevance** | `RelevanceEvaluator` | Does the answer address the question? |
| **Coherence** | `CoherenceEvaluator` | Is the response well-structured and logical? |
| **Safety** | `ContentHarmEvaluator` | Violence, self-harm, sexual, hate content |

<br/>

<div class="key">

Quality evaluators use an **LLM judge**, safety evaluators use the **Azure AI Foundry content safety service**

</div>

---

![bg fit](./img/bg-alt3.png)

# Running Evaluations

```ts
CompositeEvaluator evaluator = new([
    new GroundednessEvaluator(),
    new RelevanceEvaluator(),
    new CoherenceEvaluator(),
    new ContentHarmEvaluator(),
]);

// Safety evaluators need the Foundry content safety endpoint
ContentSafetyServiceConfiguration safetyConfig = new(credential, new Uri(endpoint));
ChatConfiguration config = safetyConfig.ToChatConfiguration(
    originalChatConfiguration: new ChatConfiguration(chatClient));

EvaluationResult result = await evaluator.EvaluateAsync(
    messages, chatResponse, config, additionalContext: [new GroundednessEvaluatorContext(context)]);
```

<div class="tip">

`Microsoft.Extensions.AI.Evaluation` — compose multiple evaluators in a single pass

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/14-foundry-evaluations.cs`

---

![bg fit](./img/bg-alt2.png)

# Key Takeaways

1. **`AsAIAgent()`** — one extension method turns any `ChatClient` into a full agent
2. **`[Description]` + `AIFunctionFactory`** — plain C# methods become LLM-callable tools
3. **`.AsAIFunction()`** — any agent can become a tool for another agent
4. **`AgentSession`** — multi-turn conversation history
5. **`BindAsExecutor()` + `WorkflowBuilder`** — compose functions and agents as directed graphs
6. **MCP / A2A / AG-UI** — expose agents via open protocols
7. **Azure AI Foundry** — server-managed agents, hosted tools, RAG, and evaluations

---

![bg fit](./img/bg-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [A2A Protocol](https://github.com/a2aproject/A2A)
- [AG-UI Protocol](https://docs.ag-ui.com) / [AG-UI Dojo](https://dojo.ag-ui.com/microsoft-agent-framework-dotnet)
- [Azure AI Foundry](https://ai.azure.com)
- [Microsoft.Extensions.AI.Evaluation](https://learn.microsoft.com/dotnet/ai/evaluation/libraries)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)

---

<!-- _class: end -->

![bg fit](./img/bg-title.png)

# **Thank You!**

<br/>

> <i class="fa-brands fa-github"></i> [nikiforovall](https://github.com/nikiforovall)
<i class="fa-brands fa-linkedin"></i> [Oleksii Nikiforov](https://www.linkedin.com/in/nikiforov-oleksii/)
<i class="fa fa-window-maximize"></i> [nikiforovall.blog](https://nikiforovall.blog/)
