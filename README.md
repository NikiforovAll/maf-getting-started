# MAF Getting Started

Presentation series for learning [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview) — the successor to Semantic Kernel and AutoGen.

## Presentations

| #   | Title                                         | Samples                                                 |
| --- | --------------------------------------------- | ------------------------------------------------------- |
| 1   | Foundations — Your First MAF Agent            | `01-hello-agent.cs`, `02-tools.cs`, `03-multi-turn.cs`  |
| 2   | Production Patterns — Memory, Workflows & MCP | `04-memory.cs`, `05-workflows.cs`, `06-agent-as-mcp.cs` |
| 3   | Hosting & Integration (TBD)                   | `07-hosting-a2a.cs`, `08-hosting-agui.cs`               |

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

## Project structure

```
src/           → Run-file C# samples (dotnet run file.cs)
slides/        → Marp presentations
scripts/       → Environment setup
_plans/        → Implementation plans
```
