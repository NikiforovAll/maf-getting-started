# Microsoft Agent Framework -  Getting Started

Samples for learning [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview) — the successor to Semantic Kernel and AutoGen.

## Prerequisites

- .NET 10 SDK
- Azure CLI (`az login`)
- Azure OpenAI resource with `gpt-4o-mini` deployment
- `Cognitive Services OpenAI Contributor` role on the resource

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

## Resources

- [MAF Documentation](https://learn.microsoft.com/en-us/agent-framework/overview)
- [MAF GitHub](https://github.com/microsoft/agent-framework)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [AG-UI Protocol](https://docs.ag-ui.com)
- [A2A Protocol](https://github.com/a2aproject/A2A)
