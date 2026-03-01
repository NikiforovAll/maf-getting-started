# MAF Getting Started

Presentation series for learning [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview) — the successor to Semantic Kernel and AutoGen.

## Presentations

| #   | Title                                         | Slides | Samples                                                 |
| --- | --------------------------------------------- | ------ | ------------------------------------------------------- |
| 1   | Foundations — Your First MAF Agent            | [view](https://nikiforovall.blog/maf-getting-started/01-foundations.html) | `01-hello-agent.cs`, `02-tools.cs`, `03-multi-turn.cs`, `04-memory.cs`, `04b-memory-custom.cs` |
| 2   | Workflows & MCP Integration                  | [view](https://nikiforovall.blog/maf-getting-started/02-workflows.html) | `05a-workflows.cs`, `05b-workflows-agents.cs`, `05c-workflows-composed.cs`, `06-agent-as-mcp.cs` |

## Prerequisites

- .NET 10 SDK
- Azure CLI (`az login`)
- Azure OpenAI resource with `gpt-4o-mini` deployment
- `Cognitive Services OpenAI Contributor` role on the resource

## Quick start

```bash
# Authenticate
az login

# Set environment
source scripts/init-env.sh

# Run a sample
dotnet run src/01-hello-agent.cs
```