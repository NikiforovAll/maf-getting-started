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

![bg fit](./img/bg-slide-alt2.png)

# Agenda

1. **What is MAF?** — The merger of Semantic Kernel + AutoGen
2. **Your First Agent** — `AzureOpenAIClient` → `.AsAIAgent()`, Run & Stream
3. **Tools** — Function tools, `[Description]`, agent-as-tool
4. **Multi-Turn Conversations** — `AgentSession`, chat history

---

![bg fit](./img/bg-section.png)

# What is **MAF**?

## Semantic Kernel + AutoGen → One Framework

---

![bg fit](./img/bg-slide-alt2.png)

# The Evolution

| Before | After |
|--------|-------|
| **Semantic Kernel** — enterprise AI orchestration | **Microsoft.Agents.AI** — unified agent runtime |
| **AutoGen** — multi-agent research framework | Single API for single & multi-agent scenarios |
| Two ecosystems, overlapping goals | Built on **Microsoft.Extensions.AI** abstractions |

<br/>

<div class="key">

**MAF** = Microsoft Agent Framework — the production-ready successor (public preview, `1.0.0-rc2`)

</div>

---

![bg fit](./img/bg-slide-alt3.png)

# Core Architecture

| Layer | Components |
|-------|-----------|
| **Your Application** | AIAgent, Tools, Sessions, Workflows |
| **Microsoft.Agents.AI** | Unified agent runtime |
| **Microsoft.Extensions.AI** | `IChatClient`, `AIFunction` |
| **Providers** | Azure OpenAI, OpenAI, Ollama, ... |

<div class="tip">

**Provider-agnostic** — swap the model provider without changing agent code

</div>

---

![bg fit](./img/bg-slide-alt1.png)

# Key Concepts

| Concept | Type | Purpose |
|---------|------|---------|
| **AIAgent** | `IAIAgent` | Core agent abstraction |
| **Tools** | `AIFunction` | Functions the agent can call |
| **Session** | `AgentSession` | Conversation state & history |
| **Run** | `RunAsync` / `RunStreamingAsync` | Execute agent with input |
| **Workflow** | `WorkflowBuilder` | Multi-agent orchestration |

---

![bg fit](./img/bg-slide-alt2.png)

# Setup — `dotnet run` File Mode

```bash
# Environment
export AZURE_OPENAI_ENDPOINT="https://your-resource.cognitiveservices.azure.com/"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"

# Run any sample directly — no .csproj needed
dotnet run src/01-hello-agent.cs
```

<br/>

<div class="tip">

**`#:package` directives** in the `.cs` file declare NuGet dependencies — `dotnet run` restores and builds automatically

</div>

---

![bg fit](./img/bg-section.png)

# Your **First Agent**

## From zero to running in 30 lines

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt3.png)

# 01-hello-agent.cs — Package Directives

```csharp
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
```

<div class="key">

**`using OpenAI.Chat`** is required — the `AsAIAgent()` extension method lives in this namespace

</div>

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# 01-hello-agent.cs — Creating an Agent

```csharp
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

![bg fit](./img/bg-slide-alt1.png)

# 01-hello-agent.cs — Run & Stream

```csharp
// Non-streaming — get the full response at once
Console.WriteLine(await agent.RunAsync("Tell me a one-sentence fun fact."));

// Streaming — process tokens as they arrive
await foreach (var update in agent.RunStreamingAsync(
    "Tell me a one-sentence fun fact."))
{
    Console.WriteLine(update);
}
```

<br/>

<div class="tip">

**`DefaultAzureCredential`** — no API keys. Uses `az login`, managed identity, or environment credentials

</div>

---

![bg fit](./img/bg-slide-alt2.png)

# The Pipeline

| Step | Call | Role |
|------|------|------|
| 1 | `AzureOpenAIClient` | Azure OpenAI provider (`Azure.AI.OpenAI`) |
| 2 | `.GetChatClient("gpt-4o-mini")` | `ChatClient` via `IChatClient` abstraction |
| 3 | `.AsAIAgent(options)` | `AIAgent` from MAF (`Microsoft.Agents.AI.OpenAI`) |
| 4 | `.RunAsync("prompt")` | Execute and get response |

---

![bg fit](./img/bg-section.png)

# **Tools**

## Giving agents the ability to act

---

![bg fit](./img/bg-slide-alt2.png)

# Function Tools — The Pattern

```csharp
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

![bg fit](./img/bg-slide-alt3.png)

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

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt1.png)

# Agent-as-Tool — Composing Agents

```csharp
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

![bg fit](./img/bg-slide-alt2.png)

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

![bg fit](./img/bg-section.png)

# **Multi-Turn** Conversations

## Maintaining context across interactions

---

![bg fit](./img/bg-slide-alt3.png)

# AgentSession — Conversation State

```csharp
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

![bg fit](./img/bg-slide-alt2.png)

# Multi-Turn in Action

```csharp
// Turn 1 — introduce context
Console.WriteLine(await agent.RunAsync(
    "My name is Alice and I love hiking.", session));

// Turn 2 — agent remembers from session history
Console.WriteLine(await agent.RunAsync(
    "What do you remember about me?", session));

// Turn 3 — agent uses accumulated context
Console.WriteLine(await agent.RunAsync(
    "Suggest a hiking destination for me.", session));
```

<br/>

<div class="tip">

Without a session, each `RunAsync` call is **stateless** — the agent has no memory of prior turns

</div>

---

![bg fit](./img/bg-slide-alt1.png)

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
- Serialization available for persistence (covered in Presentation 2)

---

![bg fit](./img/bg-slide-alt2.png)

# Key Takeaways

1. **`AsAIAgent()`** — one extension method turns any `ChatClient` into a full agent

2. **`[Description]` + `AIFunctionFactory.Create()`** — plain C# methods become LLM-callable tools

3. **`.AsAIFunction()`** — any agent can become a tool for another agent

4. **`AgentSession`** — pass to `RunAsync` to maintain multi-turn conversation history

5. **`DefaultAzureCredential`** — no API keys, subscription-level auth

---

![bg fit](./img/bg-slide-alt3.png)

# Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)
- [This repo](https://github.com/NikiforovAll/maf-getting-started)

---

![bg fit](./img/bg-title.png)

## **Next: Production Patterns**
### Memory, Workflows & MCP Integration
