---
marp: true
title: "Microsoft Agent Framework: Workflows, MCP & AG-UI"
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **Microsoft Agent Framework**
## Workflows, MCP, A2A & AG-UI

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

![bg fit](./img/bg-alt2.png)

# Agenda

1. **Workflows** — Executors, graph edges, agent pipelines
2. **MCP Integration** — Agent-as-MCP-server, dev workflow with Claude Code
3. **A2A** — Agent-to-agent communication
4. **AG-UI** — Expose agents to web UIs via HTTP + SSE

---

![bg fit](./img/bg-section.png)

# **Workflows**

## Orchestrating agents and functions as graphs

---

![bg fit](./img/bg-alt2.png)

# Three Workflow Patterns

| Pattern | Use Case | API |
|---------|----------|-----|
| **Function Workflow** | Pure data transformations, no LLM | `BindAsExecutor()` + `WorkflowBuilder` |
| **Agent Workflow** | LLM-powered multi-agent pipelines | `AgentWorkflowBuilder.BuildSequential()` |
| **Composed Workflow** | Mix functions + agents in one graph | `WorkflowBuilder` + `AddEdge()` |

<br/>

<div class="key">

Workflows are **directed graphs** — nodes are executors (functions or agents), edges define data flow

</div>

---


![bg fit](./img/bg-alt3.png)

# Function Workflow — Pure Transformations

```ts
// Bind plain functions as workflow executors
Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

Func<string, string> reverseFunc = s => string.Concat(s.Reverse());
var reverse = reverseFunc.BindAsExecutor("ReverseTextExecutor");

// Build graph: uppercase → reverse
WorkflowBuilder builder = new(uppercase);
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
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

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Workflow Building Blocks

| Building Block | API                                            | Description                      |
| -------------- | ---------------------------------------------- | -------------------------------- |
| **Executor**   | `Func<T,R>.BindAsExecutor()`                   | A node in the workflow graph     |
| **Edge**       | `builder.AddEdge(A, B)`                        | Connects two executors (A → B)   |
| **Output**     | `builder.WithOutputFrom(B)`                    | Designates the final output node |
| **Run**        | `InProcessExecution.RunAsync(workflow, input)` | Executes the workflow            |
| **StreamingRun** | `InProcessExecution.RunStreamingAsync()`     | Executes with streaming events   |
| **Events**     | `ExecutorCompletedEvent`, `AgentResponseUpdateEvent` | Emitted per completed node |

---

<style scoped>
section {
  font-size: 26px;
}
</style>

![bg fit](./img/bg-alt1.png)

# Executors — Generic Types Define the Contract

```ts
// Func<TInput, TOutput> — sync, wrapped to ValueTask internally
Func<string, string> upper = s => s.ToUpperInvariant();
var upperExec = upper.BindAsExecutor("Upper");

// Func<TInput, ValueTask<TOutput>> — async (I/O, LLM calls)
Func<string, ValueTask<string>> rewrite = async text =>
    (await myAgent.RunAsync(text)).ToString();
var rewriteExec = rewrite.BindAsExecutor("Rewriter");

// Composing: TOutput of node A must match TInput of node B
WorkflowBuilder builder = new(upperExec);
builder.AddEdge(upperExec, rewriteExec);  // string → string ✓
builder.WithOutputFrom(rewriteExec);
```

<div class="key">

**`TInput`/`TOutput`** define the executor contract — sync `Func<T,R>` is auto-wrapped to `ValueTask` by the framework

</div>

---


![bg fit](./img/bg-alt3.png)

# Composed Workflow — Code

```ts
// Function executor — mask emails with regex
Func<string, string> maskEmails = text =>
    Regex.Replace(text, @"[\w.-]+@[\w.-]+\.\w+", "[EMAIL_REDACTED]");
var maskExecutor = maskEmails.BindAsExecutor("MaskEmails");

// Agent executor — LLM rewrites text, keeps redactions intact
Func<string, ValueTask<string>> rewriteFunc = async text =>
    (await rewriter.RunAsync(text)).ToString();
var rewriteExecutor = rewriteFunc.BindAsExecutor("Rewriter");

// Function executor — validate no emails leaked through LLM
Func<string, string> validate = text =>
    Regex.IsMatch(text, @"[\w.-]+@[\w.-]+\.\w+")
        ? "VALIDATION FAILED" : $"CLEAN\n\n{text}";
var validateExecutor = validate.BindAsExecutor("ValidateNoLeaks");
```

---

![bg fit](./img/bg-alt1.png)

# Composed Workflow — Graph

```ts
// Build graph: mask → rewrite → validate
WorkflowBuilder builder = new(maskExecutor);
builder.AddEdge(maskExecutor, rewriteExecutor);
builder.AddEdge(rewriteExecutor, validateExecutor);
builder.WithOutputFrom(validateExecutor);
var workflow = builder.Build();
```

| Step | Executor | Type | Output |
|------|----------|------|--------|
| 1 | MaskEmails | `Func` | `"contact [EMAIL_REDACTED] for..."` |
| 2 | Rewriter | `AIAgent` | Naturally rewritten text |
| 3 | ValidateNoLeaks | `Func` | `"CLEAN — no emails detected"` |

---

![bg fit](./img/bg-alt2.png)

# Composed Workflow — Execution

```ts
await using Run run = await InProcessExecution.RunAsync(workflow, input);

foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine(
            $"[{executorComplete.ExecutorId}]: {executorComplete.Data}");
    }
}
```

```
[MaskEmails]: "Hi team, contact Alice at [EMAIL_REDACTED] for the Q3 report..."
[Rewriter]: "Hi team, please reach out to Alice at [EMAIL_REDACTED] for..."
[ValidateNoLeaks]: "CLEAN - no emails detected"
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

# 06-agent-as-mcp.cs — Server Setup

```ts
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

![bg fit](./img/bg-alt2.png)

# 06-agent-as-mcp.cs — Exposing as MCP Tools

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

// Discover tools and pass to agent
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

## `dotnet run src/06-agent-as-mcp.cs`
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

# 08a — A2A Server

```ts
#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Agents.AI.Hosting.A2A.AspNetCore@1.0.0-preview.260225.1
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2

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

# 08b — A2A Client

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

## `dotnet run src/08a-agent-as-a2a-server.cs`
## `dotnet run src/08b-agent-as-a2a-client.cs`

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

# 07-agent-as-agui.cs — AG-UI Server

```ts
#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore@1.0.0-preview.260225.1
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2

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

# 07b — AG-UI Client

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

## `dotnet run src/07-agent-as-agui.cs`
## `dotnet run src/07b-agent-as-agui-client.cs`

---

![bg fit](./img/bg-alt2.png)

# Key Takeaways

1. **`BindAsExecutor()`** — turn any `Func<T,R>` into a workflow node

2. **`AgentWorkflowBuilder.BuildSequential()`** — chain agents into pipelines with one call

3. **`WorkflowBuilder` + `AddEdge()`** — compose function and agent executors in a single graph

4. **`McpServerTool.Create(agent.AsAIFunction())`** — expose agents as MCP tools in two lines

5. **`MapA2A()` + `AgentCard`** — agents discover and call each other over HTTP

6. **`AddAGUI()` + `MapAGUI()`** — expose agents to web UIs via HTTP+SSE in two lines

---

![bg fit](./img/bg-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [AG-UI Protocol](https://docs.ag-ui.com)
- [AG-UI in MAF](https://learn.microsoft.com/agent-framework/integrations/ag-ui/getting-started)
- [AG-UI Dojo (MAF + CopilotKit)](https://dojo.ag-ui.com/microsoft-agent-framework-dotnet)
- [A2A Protocol](https://github.com/a2aproject/A2A)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)
