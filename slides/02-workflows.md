---
marp: true
title: "Microsoft Agent Framework: Workflows & MCP"
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **Microsoft Agent Framework**
## Workflows & MCP Integration

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

<style scoped>
section {
  font-size: 28px;
}
</style>

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
// Execute
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

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-alt2.png)

# Agent Workflow — Sequential Pipeline

```ts
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

var agentWorkflow = AgentWorkflowBuilder.BuildSequential("story-pipeline", [writer, critic]);
```

---

![bg fit](./img/bg-alt3.png)

# Agent Workflow — Execution

```ts
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

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/05b-workflows-agents.cs`

---

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

![bg fit](./img/bg-alt2.png)

# Workflow Building Blocks

| Building Block | API                                            | Description                      |
| -------------- | ---------------------------------------------- | -------------------------------- |
| **Executor**   | `Func<T,R>.BindAsExecutor()`                   | A node in the workflow graph     |
| **Edge**       | `builder.AddEdge(A, B)`                        | Connects two executors (A → B)   |
| **Output**     | `builder.WithOutputFrom(B)`                    | Designates the final output node |
| **Run**        | `InProcessExecution.RunAsync(workflow, input)` | Executes the workflow            |
| **Events**     | `ExecutorCompletedEvent`                       | Emitted per completed node       |

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

<style scoped>
section {
  font-size: 28px;
}
</style>

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

<div class="tip">

**`.AsAIFunction()` → `McpServerTool.Create()`** — two calls to go from agent to MCP tool

</div>

---

<style scoped>
section {
  font-size: 28px;
}
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

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-alt2.png)

# Agent as MCP Client

```ts
// Connect to a remote MCP server over HTTP
await using var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new()
    {
        Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
        Name = "Microsoft Learn MCP",
    }));

// Discover tools and pass to agent
IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();

AIAgent agent = client.GetChatClient(deploymentName)
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

---

![bg fit](./img/bg-alt2.png)

# Key Takeaways

1. **`BindAsExecutor()`** — turn any `Func<T,R>` into a workflow node

2. **`AgentWorkflowBuilder.BuildSequential()`** — chain agents into pipelines with one call

3. **`WorkflowBuilder` + `AddEdge()`** — compose function and agent executors in a single graph

4. **`McpServerTool.Create(agent.AsAIFunction())`** — expose agents as MCP tools in two lines

5. **`.mcp.json`** — declarative config for MCP client discovery

---

![bg fit](./img/bg-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)
