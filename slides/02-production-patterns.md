---
marp: true
title: "Microsoft Agent Framework: Production Patterns"
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **Microsoft Agent Framework**
## Production Patterns — Memory, Workflows & MCP

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

![bg fit](./img/bg-slide-alt2.png)

# Agenda

1. **Memory & Persistence** — Chat history, serialization, session restore
2. **Workflows** — Executors, graph edges, agent pipelines
3. **MCP Integration** — Agent-as-MCP-server, dev workflow with Claude Code

---

![bg fit](./img/bg-section.png)

# **Memory** & Persistence

## Keeping agents stateful across sessions

---

![bg fit](./img/bg-slide-alt3.png)

# The Problem

- **Session A** (in memory): User says *"My name is Alice"* — Agent remembers
- **Process restart** — memory is lost
- **Session B** (new process): User asks *"What is my name?"* — Agent has no idea

<br/>

<div class="warning">

**Default `InMemoryChatHistoryProvider`** — conversation state lives only in process memory

</div>

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# 04-memory.cs — In-Memory History

```csharp
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

![bg fit](./img/bg-slide-alt1.png)

# Session Serialization

```csharp
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

![bg fit](./img/bg-slide-alt2.png)

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
- **Custom**: Implement `IChatHistoryProvider` for database-backed history

---

![bg fit](./img/bg-section.png)

# **Workflows**

## Orchestrating agents and functions as graphs

---

![bg fit](./img/bg-slide-alt2.png)

# Two Workflow Patterns

| Pattern | Use Case | API |
|---------|----------|-----|
| **Function Workflow** | Pure data transformations, no LLM | `BindAsExecutor()` + `WorkflowBuilder` |
| **Agent Workflow** | LLM-powered multi-agent pipelines | `AgentWorkflowBuilder.BuildSequential()` |

<br/>

<div class="key">

Workflows are **directed graphs** — nodes are executors (functions or agents), edges define data flow

</div>

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt3.png)

# Function Workflow — Pure Transformations

```csharp
// Bind plain functions as workflow executors
Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

Func<string, string> reverseFunc = s => string.Concat(s.Reverse());
var reverse = reverseFunc.BindAsExecutor("ReverseTextExecutor");

// Build graph: uppercase → reverse
WorkflowBuilder builder = new(uppercase);
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
var workflow = builder.Build();

// Execute
await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
```

---

![bg fit](./img/bg-slide-alt1.png)

# Function Workflow — Execution

| Step | Executor | Output |
|------|----------|--------|
| Input | — | `"Hello, World!"` |
| 1 | UppercaseExecutor | `"HELLO, WORLD!"` |
| 2 | ReverseTextExecutor | `"!DLROW ,OLLEH"` |

```csharp
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine(
            $"{executorComplete.ExecutorId}: {executorComplete.Data}");
    }
}
```

<div class="tip">

**No LLM calls** — pure function workflows for deterministic data pipelines

</div>

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# Agent Workflow — Sequential Pipeline

```csharp
AIAgent writer = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You write short creative stories in 2-3 sentences.",
        name: "Writer"
    );

AIAgent critic = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You review stories and give brief constructive feedback "
            + "in 1-2 sentences.",
        name: "Critic"
    );

var agentWorkflow = AgentWorkflowBuilder.BuildSequential(
    "story-pipeline", [writer, critic]);
```

---

![bg fit](./img/bg-slide-alt3.png)

# Agent Workflow — Execution

```csharp
await using Run agentRun = await InProcessExecution.RunAsync(
    agentWorkflow, "Write a story about a robot learning to paint.");

foreach (WorkflowEvent evt in agentRun.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine(
            $"[{executorComplete.ExecutorId}]: {executorComplete.Data}");
    }
}
```

```
[Writer]: "A small robot named Pixel discovered an abandoned art studio..."
[Critic]: "The story has a charming premise. Consider adding sensory details..."
```

---

![bg fit](./img/bg-slide-alt1.png)

# Workflow Building Blocks

| Building Block | API | Description |
|---------------|-----|-------------|
| **Executor** | `Func<T,R>.BindAsExecutor()` or `AIAgent` | A node in the workflow graph |
| **Edge** | `builder.AddEdge(A, B)` | Connects two executors (A then B) |
| **Output** | `builder.WithOutputFrom(B)` | Designates the final output node |
| **Run** | `InProcessExecution.RunAsync(workflow, input)` | Executes the workflow |
| **Events** | `ExecutorCompletedEvent` | Emitted per completed node |

---

![bg fit](./img/bg-section.png)

# **MCP** Integration

## Agents as Model Context Protocol servers

---

![bg fit](./img/bg-slide-alt2.png)

# What is MCP?

**Model Context Protocol** — open standard for connecting AI models to external tools and data

| Side | Component | Transport |
|------|-----------|-----------|
| **Client** | Claude, VS Code, any MCP client | stdio |
| **Server** | Your .NET agents exposed as MCP tools | stdio |

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

![bg fit](./img/bg-slide-alt3.png)

# 06-agent-as-mcp.cs — Server Setup

```csharp
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
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

![bg fit](./img/bg-slide-alt2.png)

# 06-agent-as-mcp.cs — Exposing as MCP Tools

```csharp
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

<div class="tip">

**`.AsAIFunction()` → `McpServerTool.Create()`** — two calls to go from agent to MCP tool

</div>

---

![bg fit](./img/bg-slide-alt1.png)

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

![bg fit](./img/bg-slide-alt2.png)

# Dev Workflow with MCP

1. Write agents in `.cs` files
2. Wrap with `McpServerTool.Create(agent.AsAIFunction())`
3. Expose via stdio: `AddMcpServer().WithStdioServerTransport()`
4. Configure `.mcp.json` in repo root
5. Claude Code / VS Code auto-discovers and calls your agents

<br/>

<div class="key">

**Your agents become first-class tools** in any MCP client — Claude can call your WeatherAgent directly

</div>

---

![bg fit](./img/bg-slide-alt3.png)

# The Full Picture

1. **Claude Code** receives: *"What's the weather in Amsterdam?"*
2. **Discovers** available MCP tools: `Joker`, `WeatherAgent`
3. **Calls** `WeatherAgent` via stdio transport
4. **MCP Server** (`06-agent-as-mcp`) routes to `WeatherAgent` which calls `GetWeather()`
5. **Result** flows back through stdio to Claude Code

---

![bg fit](./img/bg-slide-alt2.png)

# Key Takeaways

1. **`SerializeSessionAsync`** — portable session state, store anywhere, restore anytime

2. **`BindAsExecutor()`** — turn any `Func<T,R>` into a workflow node

3. **`AgentWorkflowBuilder.BuildSequential()`** — chain agents into pipelines with one call

4. **`McpServerTool.Create(agent.AsAIFunction())`** — expose agents as MCP tools in two lines

5. **`.mcp.json`** — declarative config for MCP client discovery

---

![bg fit](./img/bg-slide-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)

---

![bg fit](./img/bg-title.png)

## **Next: Hosting & Integration**
### A2A, AG-UI & .NET Aspire
