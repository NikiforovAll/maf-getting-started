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
7. **Evaluations** — Quality, safety, and self-reflection

---

<!-- _class: chapter -->

![bg fit](./img/bg-section.png)

# Demo

## A quick tour of Azure AI Foundry

---


![bg fit](./img/bg-section.png)

# **Why Foundry?**

### Managed platform for building, deploying, and monitoring AI agents at scale

---

<style scoped>
th,td {font-size: 20px;}
</style>

![bg fit](./img/bg-alt2.png)

# Azure OpenAI vs MAF + Azure AI Foundry

| | MAF | Azure AI Foundry |
|--|--|--|
| **Schema Versioning** | - | Definition stored Azure-side (named + versioned) |
| **Tools** | Server-side `AIFunction` | Server-side + **hosted** (Code, Search, Web) |
| **Memory** | `InMemoryChatHistoryProvider` | Managed **Memory Stores** |
| **RAG** | Build your own | Hosted **vector stores** + file search tool |
| **Evaluation** | Build your own | Built-in **quality + safety** evaluators |

<br/>

<div class="key">

Same **`AIAgent`** / **`RunAsync()`** API surface — the abstraction doesn't change

</div>

---

![bg fit](./img/bg-alt3.png)

# What Changes?

**New package:**
```
#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc5
#:package Azure.AI.Projects@2.0.0-beta.2
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
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "FoundryBasicsAgent",
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions = "You are a friendly assistant. Keep your answers brief.",
        }));
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);
```

<div class="key">

Agents are **server-side resources** — named, versioned, persisted in Foundry

</div>

---

![bg fit](./img/bg-alt3.png)

# Retrieve & Run — Same API

```ts
// Retrieve latest version by name
AgentRecord record = await aiProjectClient.Agents.GetAgentAsync("FoundryBasicsAgent");
AIAgent retrieved = aiProjectClient.AsAIAgent(record);

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

![bg fit](./img/bg-alt3.png)

# Agent Versioning

```ts
// Each CreateAgentVersionAsync call creates a new version
AgentVersion v1 = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "MyAgent",
    options: new (new PromptAgentDefinition("gpt-4o-mini") {
        Instructions = "You are helpful."
    }));

AgentVersion v2 = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "MyAgent",
    options: new (new PromptAgentDefinition("gpt-4o-mini") {
        Instructions = "You are extremely helpful and concise."
    }));

// GetAgentAsync returns the latest version as an AgentRecord
AgentRecord latestRecord = await aiProjectClient.Agents.GetAgentAsync("MyAgent");
AIAgent latest = aiProjectClient.AsAIAgent(latestRecord);
```

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
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "FoundryBasicsAgent",
    options: new (new PromptAgentDefinition(deploymentName) { Instructions = "You are a friendly assistant." }));
AIAgent agent = aiProjectClient
    .AsAIAgent(agentVersion)
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
// Get agent client
AIAgent foundryAgent = aiProjectClient.AsAIAgent(agentVersion);
ChatClientAgent agent = foundryAgent.GetService<ChatClientAgent>()!;

// Create a server-side conversation
ProjectConversationsClient conversationsClient = aiProjectClient
    .GetProjectOpenAIClient()
    .GetProjectConversationsClient();

ProjectConversation conversation = await conversationsClient.CreateProjectConversationAsync();

// Link session to the conversation — history stored server-side
AgentSession session = await agent.CreateSessionAsync(conversation.Id);

// Run
Console.WriteLine(await agent.RunAsync("My name is Oleksii.", session));
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

// → "Your name is Oleksii."
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
{
    return $"The weather in {location} is sunny with a high of 22°C.";
}

AIFunction getWeather = AIFunctionFactory.Create(GetWeather);
AITool[] tools = [getWeather];
```

---

![bg fit](./img/bg-alt2.png)

# Creating Agent with Tools

```ts
// Register schema server-side (declarative) + provide invocable impl client-side
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "WeatherAgent",
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions = "You are a helpful assistant with weather tools.",
            Tools = { getWeather.AsOpenAIResponseTool() },
        }
    )
);

AIAgent agent = aiProjectClient.AsAIAgent(agentVersion, tools: tools);
```

---

![bg fit](./img/bg-alt3.png)

# Retrieving Agents with Tools

```ts
// IMPORTANT: Server only stores the tool schema (JSON Schema)
// You must provide invocable tools when retrieving
AgentRecord record = await aiProjectClient.Agents.GetAgentAsync("WeatherAgent");
AIAgent existing = aiProjectClient.AsAIAgent(
    record,
    tools: [AIFunctionFactory.Create(GetWeather)]
);

Console.WriteLine(await existing.RunAsync("What's the weather in Kyiv?"));
```

<br/>

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
| **Examples** | `AIFunctionFactory.Create(...)` | `ResponseTool.CreateCodeInterpreterTool/CreateWebSearchTool/CreateFileSearchTool` |
| **Use case** | Custom business logic | Python execution, web search, file search |

<br/>

<div class="key">

Hosted tools run **server-side** — no local dependencies, no infrastructure to manage

</div>

---

![bg fit](./img/bg-alt3.png)

# Code Interpreter — Python Sandbox

```ts
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "MathTutor",
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions = "You are a math tutor. Write and run Python code to solve problems.",
            Tools = { ResponseTool.CreateCodeInterpreterTool(
                new CodeInterpreterToolContainer(
                    CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(fileIds: []))) },
        }));
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);

AgentResponse response = await agent.RunAsync(
    "Solve x^3 - 6x^2 + 11x - 6 = 0. Plot the function and mark the roots.");
```

<br/>

<div class="tip">

`ResponseTool.CreateCodeInterpreterTool(...)` — the agent writes Python, Foundry runs it in a sandbox, returns results + files

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
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "WebSearchAgent",
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions = "Search the web to answer questions accurately. Cite your sources.",
            Tools = { ResponseTool.CreateWebSearchTool() },
        }));
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);

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

`ResponseTool.CreateWebSearchTool()` — agent searches the web autonomously and returns **annotated citations**

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
| 3. **Create agent** | `CreateAgentVersionAsync` + `ResponseTool.CreateFileSearchTool` | Agent grounded on your data |
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

// Wait for file ingestion to complete before querying
while ((await vectorStoresClient.GetVectorStoreAsync(vectorStore.Value.Id)).Value.Status
    != VectorStoreStatus.Completed)
{
    await Task.Delay(500);
}
```

---

![bg fit](./img/bg-alt1.png)

# Create Agent with File Search

```ts
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: "RAGAgent",
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions = "Answer questions using the product catalog. Cite the source.",
            Tools = { ResponseTool.CreateFileSearchTool(vectorStoreIds: [vectorStoreId]) },
        }));
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);

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
AgentRecord workflowRecord = await aiProjectClient.Agents.GetAgentAsync(WorkflowName);
ChatClientAgent workflowAgent = (ChatClientAgent)aiProjectClient.AsAIAgent(workflowRecord);

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

1. **`AIProjectClient.Agents.CreateAgentVersionAsync()` + `AsAIAgent()`** — same `AIAgent` API, server-managed lifecycle with versioning

2. **`.UseOpenTelemetry()` + Aspire** — client spans correlated with Foundry server traces

3. **`ResponseTool.CreateCodeInterpreterTool()`** / **`CreateWebSearchTool()`** — zero-infrastructure hosted tools

4. **File upload → vector store → `ResponseTool.CreateFileSearchTool()`** — RAG in ~10 lines

5. **`GroundednessEvaluator` + `ContentHarmEvaluator`** — evaluate quality and safety before production

---

![bg fit](./img/bg-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [Azure AI Foundry](https://ai.azure.com)
- [Azure.AI.Projects SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.projects-readme)
- [Microsoft.Extensions.AI.Evaluation](https://learn.microsoft.com/dotnet/ai/evaluation/libraries)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)
