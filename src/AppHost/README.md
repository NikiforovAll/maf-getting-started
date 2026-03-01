# AppHost

Aspire AppHost that references an **existing** Azure OpenAI resource. The `gpt-4o-mini` deployment will be auto-created if it doesn't exist (Bicep is declarative).

## Prerequisites

- Azure subscription with an Azure OpenAI resource
- `az login` authenticated to the correct subscription
- Bicep CLI: `az bicep install`

## Setup

1. Switch to the subscription containing your Azure OpenAI resource:

```bash
az account set --subscription <subscription-id>
```

2. Find your resource name and resource group:

```bash
az cognitiveservices account list --query "[].{name:name, rg:resourceGroup}" -o table
```

3. Configure via user secrets:

```bash
dotnet user-secrets init --project src/AppHost
dotnet user-secrets set "Azure:OpenAI:Name" "your-resource-name" --project src/AppHost
dotnet user-secrets set "Azure:OpenAI:ResourceGroup" "your-rg" --project src/AppHost
```

If not set, the Aspire dashboard will prompt for values on first run.

## Run

```bash
dotnet run --project src/AppHost
```
