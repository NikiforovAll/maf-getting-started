---
marp: true
title: "Microsoft Agent Framework: Azure AI Foundry"
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **Microsoft Agent Framework**
## Azure AI Foundry

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

1. **Why Foundry?** — From plain Azure OpenAI to managed agents
2. **First Foundry Agent** — `AIProjectClient` + versioning
3. **Observability** — OpenTelemetry + Aspire + Foundry traces
4. **Function Tools** — Same pattern, server-side management
5. **Hosted Tools** — Code Interpreter, Web Search
6. **RAG via Foundry** — File upload, vector stores, file search
7. **Memory** — Cross-session recall with `MemorySearchTool`
8. **Evaluations** — Quality, safety, and self-reflection

---

![bg fit](./img/bg-section.png)

# **Why Foundry?**

## From Azure OpenAI to managed agents

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Azure OpenAI vs Azure AI Foundry

| | Azure OpenAI (Sessions 1–2) | Azure AI Foundry |
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
#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc2
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

<div class="tip">

Agent definitions are **immutable** after creation — create a new version to change instructions or tools

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/09-foundry-basics.cs`

---

![bg fit](./img/bg-section.png)

# **Observability**

## OpenTelemetry + Aspire Dashboard + Foundry Traces

---

![bg fit](./img/bg-alt2.png)

# Two Sides of the Trace

| | Client-side (OTEL) | Server-side (Foundry) |
|--|--|--|
| **What** | Agent spans, chat calls, duration | Token counts, cost, response IDs |
| **Where** | Aspire Dashboard / any OTLP backend | Foundry Portal → Traces tab |
| **How** | `.UseOpenTelemetry()` + OTLP exporter | Automatic — built into Foundry |

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

<div class="tip">

Print the **Trace ID** → find the same trace in Foundry Portal with token counts and cost

</div>

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

<div class="tip">

`HostedCodeInterpreterTool` — the agent writes Python, Foundry runs it in a sandbox, returns results + files

</div>

---

![bg fit](./img/bg-alt1.png)

# Extracting Code & Results

```ts
// Get the Python code that was executed
var toolCall = response.Messages.SelectMany(m => m.Contents)
    .OfType<CodeInterpreterToolCallContent>().FirstOrDefault();

if (toolCall?.Inputs?.OfType<DataContent>().FirstOrDefault() is { } code)
    Console.WriteLine(Encoding.UTF8.GetString(code.Data.ToArray()));

// Get the execution result
var toolResult = response.Messages.SelectMany(m => m.Contents)
    .OfType<CodeInterpreterToolResultContent>().FirstOrDefault();

if (toolResult?.Outputs?.OfType<TextContent>().FirstOrDefault() is { } result)
    Console.WriteLine(result.Text);
```

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

<br/>

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

# **Memory**

## Cross-session recall with Foundry Memory Stores

---

![bg fit](./img/bg-alt2.png)

# Memory Search Tool

```ts
// Memory store must be pre-created via Azure Portal
string memoryStoreName = Environment.GetEnvironmentVariable("AZURE_AI_MEMORY_STORE_ID")!;

// Scope isolates memories per user
string userScope = $"user_{Environment.MachineName}";

MemorySearchTool memorySearchTool = new(memoryStoreName, userScope)
{
    UpdateDelay = 1,  // seconds before new memories are indexed
};

AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName, name: "MemoryAgent",
    instructions: "Remember user preferences across conversations.",
    tools: [((ResponseTool)memorySearchTool).AsAITool()]);
```

---

![bg fit](./img/bg-alt3.png)

# Cross-Session Recall

```ts
// Session 1: Share preferences
await agent.RunAsync("My name is Alex. I prefer dark mode in all my tools.");

// Wait for memory indexing
await Task.Delay(3000);

// Session 2: Test recall — agent remembers without chat history
await agent.RunAsync("What do you know about me?");
// → "Your name is Alex and you prefer dark mode."
```

<div class="key">

Memories **persist beyond sessions** — user-scoped, automatically extracted and indexed by Foundry

</div>

---

![bg fit](./img/bg-alt1.png)

# Inspecting Memory Results

```ts
foreach (var message in response.Messages)
{
    if (message.RawRepresentation is MemorySearchToolCallResponseItem memoryResult)
    {
        foreach (var result in memoryResult.Results)
        {
            Console.WriteLine($"  [{result.MemoryItem.Scope}] {result.MemoryItem.Content}");
        }
    }
}
```

<div class="tip">

Memory stores are **long-lived resources** — they persist even after the agent is deleted

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/13-foundry-memory.cs`

---

![bg fit](./img/bg-section.png)

# **Evaluations**

## Quality, safety, and self-reflection

---

![bg fit](./img/bg-alt2.png)

# Why Evaluate?

**Before production, you need to know:**

| Dimension | Evaluator | What it measures |
|-----------|-----------|-----------------|
| **Groundedness** | `GroundednessEvaluator` | Are answers grounded in provided context? |
| **Relevance** | `RelevanceEvaluator` | Does the answer address the question? |
| **Coherence** | `CoherenceEvaluator` | Is the response well-structured and logical? |
| **Safety** | `ContentHarmEvaluator` | Does the response contain harmful content? |

<br/>

<div class="key">

`Microsoft.Extensions.AI.Evaluation` — LLM-as-judge for quality, Azure AI Foundry for safety

</div>

---

<style scoped>
section {font-size: 26px;}
</style>

![bg fit](./img/bg-alt3.png)

# Self-Reflection Loop

Based on [Reflexion (NeurIPS 2023)](https://arxiv.org/abs/2303.11366) — verbal reinforcement learning

```ts
GroundednessEvaluator evaluator = new();

for (int i = 0; i < 3; i++)
{
    AgentResponse agentResponse = await agent.RunAsync(currentPrompt, session);
    EvaluationResult result = await evaluator.EvaluateAsync(
        messages, chatResponse, config, additionalContext: [groundingContext]);

    double score = result.Get<NumericMetric>(
        GroundednessEvaluator.GroundednessMetricName).Value ?? 0;

    if (score >= 4.0) break;

    currentPrompt = $"Your answer scored {score}/5. Improve grounding.";
}
```

---

![bg fit](./img/bg-alt1.png)

# Combined Quality + Safety

```ts
CompositeEvaluator evaluator = new([
    new RelevanceEvaluator(),
    new CoherenceEvaluator(),
    new ContentHarmEvaluator(),
]);

// Safety evaluators use Azure AI Foundry content safety endpoint
ContentSafetyServiceConfiguration safetyConfig = new(
    credential: credential, endpoint: new Uri(endpoint));

ChatConfiguration config = safetyConfig.ToChatConfiguration(
    originalChatConfiguration: new ChatConfiguration(chatClient));

EvaluationResult result = await evaluator.EvaluateAsync(
    messages, chatResponse, config);
```

---

![bg fit](./img/bg-alt2.png)

# Evaluation Output

```
=== Quality + Safety Evaluation ===

  Relevance               Score: 5.0/5  Rating: Good
  Coherence               Score: 4.5/5  Rating: Good
  ContentHarm.Violence    Value: False   Rating: Good    Failed: False
  ContentHarm.SelfHarm    Value: False   Rating: Good    Failed: False
  ContentHarm.Sexual      Value: False   Rating: Good    Failed: False
  ContentHarm.HateUnfair  Value: False   Rating: Good    Failed: False
```

<div class="tip">

Quality evaluators use an **LLM judge**, safety evaluators use the **Azure AI Foundry content safety service**

</div>

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## `dotnet run src/14-foundry-evaluations.cs`

---

![bg fit](./img/bg-alt2.png)

# Key Takeaways

1. **`AIProjectClient.CreateAIAgentAsync()`** — same `AIAgent` API, server-managed lifecycle

2. **`.UseOpenTelemetry()` + Aspire** — client spans correlated with Foundry server traces

3. **`HostedCodeInterpreterTool`** / **`HostedWebSearchTool`** — zero-infrastructure hosted tools

4. **File upload → vector store → `HostedFileSearchTool`** — RAG in ~10 lines

5. **`MemorySearchTool`** — cross-session memory, user-scoped, Foundry-managed

6. **`GroundednessEvaluator` + `ContentHarmEvaluator`** — evaluate quality and safety before production

---

![bg fit](./img/bg-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [Azure AI Foundry](https://ai.azure.com)
- [Azure.AI.Projects SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.projects-readme)
- [Microsoft.Extensions.AI.Evaluation](https://learn.microsoft.com/dotnet/ai/evaluation/libraries)
- [Reflexion Paper (NeurIPS 2023)](https://arxiv.org/abs/2303.11366)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)
