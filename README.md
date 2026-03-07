# Microsoft Agent Framework -  Getting Started

Samples for learning [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview) — the successor to Semantic Kernel and AutoGen.

## Prerequisites

- .NET 10 SDK
- Azure CLI (`az login`)
- Azure OpenAI resource with `gpt-4o-mini` deployment
- `Cognitive Services OpenAI Contributor` role on the resource
- Azure AI Foundry project (for Session 3 samples)
- `Azure AI Developer` role on the Foundry project

## Quick start

```bash
az login
source scripts/init-env.sh
dotnet run src/01-hello-agent.cs
```

## Samples

| #   | Sample                        | Description                                  |
| --- | ----------------------------- | -------------------------------------------- |
| 01  | `01-hello-agent.cs`           | Minimal agent — prompt in, response out      |
| 02  | `02-tools.cs`                 | Tool calling and agent-as-tool orchestration |
| 03  | `03-multi-turn.cs`            | Multi-turn conversation loop                 |
| 04  | `04-memory.cs`                | Built-in chat history                        |
| 04b | `04b-memory-custom.cs`        | Custom memory provider                       |
| 04c | `04c-memory-session-aware.cs` | Session-aware memory across agents           |
| 05a | `05a-workflows.cs`            | Function-based workflow graph                |
| 05b | `05b-workflows-agents.cs`     | Agent-based sequential pipeline              |
| 05c | `05c-workflows-composed.cs`   | Mixed function + agent workflow              |
| 06  | `06-agent-as-mcp.cs`          | Expose agents as MCP server (stdio)          |
| 06b | `06b-agent-as-mcp-client.cs`  | Consume remote MCP tools (Microsoft Learn)   |
| 07  | `07-agent-as-agui.cs`         | AG-UI server — expose agent via HTTP+SSE     |
| 07b | `07b-agent-as-agui-client.cs` | AG-UI client — streaming console chat        |
| 08a | `08a-agent-as-a2a-server.cs`  | A2A server — agent with discovery card       |
| 08b | `08b-agent-as-a2a-client.cs`  | A2A client — call remote agent               |
| 09  | `09-foundry-basics.cs`        | First Foundry agent — create, version, run   |
| 10  | `10-foundry-tools.cs`         | Function tools on Foundry                    |
| 11a | `11a-foundry-code-interpreter.cs` | Code Interpreter — Python sandbox        |
| 11b | `11b-foundry-web-search.cs`   | Web Search — real-time queries + citations   |
| 12  | `12-foundry-rag.cs`           | RAG — file upload, vector store, file search |
| 13  | `13-foundry-memory.cs`        | Memory — cross-session recall via Foundry    |
| 14  | `14-foundry-evaluations.cs`   | Evaluations — quality, safety, self-reflection |

### Client/server samples

Some samples run as pairs:

```bash
# AG-UI: server + client
dotnet run src/07-agent-as-agui.cs          # Terminal 1
dotnet run src/07b-agent-as-agui-client.cs  # Terminal 2

# A2A: server + client
dotnet run src/08a-agent-as-a2a-server.cs   # Terminal 1
dotnet run src/08b-agent-as-a2a-client.cs   # Terminal 2
```

## Presentations

| # | Title | Slides | Samples |
|---|-------|--------|---------|
| 1 | Foundations | [01-foundations](https://nikiforovall.blog/maf-getting-started/01-foundations.html) | 01 – 04c |
| 2 | Workflows, MCP, A2A & AG-UI | [02-workflows](https://nikiforovall.blog/maf-getting-started/02-workflows.html) | 05a – 08b |
| 3 | Azure AI Foundry | [03-azure-ai-foundry](https://nikiforovall.blog/maf-getting-started/03-azure-ai-foundry.html) | 09 – 14 |

## Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF GitHub](https://github.com/microsoft/agent-framework)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [AG-UI Protocol](https://docs.ag-ui.com)
- [A2A Protocol](https://github.com/a2aproject/A2A)
