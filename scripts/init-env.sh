#!/bin/bash
# Set environment variables for MAF samples
# Usage: source scripts/init-env.sh

# Azure resource identifiers (used by Aspire AppHost)
export AZURE_OPENAI_NAME="${AZURE_OPENAI_NAME:-oleksii-nikiforov-7668-resource}"
export AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-rg-Oleksii_Nikiforov-7668}"

# Azure OpenAI (Sessions 1-2)
export AZURE_OPENAI_ENDPOINT="${AZURE_OPENAI_ENDPOINT:-https://${AZURE_OPENAI_NAME}.cognitiveservices.azure.com/}"
export AZURE_OPENAI_DEPLOYMENT_NAME="${AZURE_OPENAI_DEPLOYMENT_NAME:-gpt-4o-mini}"

# Azure AI Foundry (Session 3)
export AZURE_AI_PROJECT_ENDPOINT="${AZURE_AI_PROJECT_ENDPOINT:-https://${AZURE_OPENAI_NAME}.services.ai.azure.com/api/projects/oleksii_nikiforov-7668}"
export AZURE_AI_MODEL_DEPLOYMENT_NAME="${AZURE_AI_MODEL_DEPLOYMENT_NAME:-gpt-4o-mini}"

# OpenTelemetry — Aspire dashboard OTLP HTTP endpoint (from AppHost launchSettings.json, https profile)
export OTEL_EXPORTER_OTLP_ENDPOINT="${OTEL_EXPORTER_OTLP_ENDPOINT:-https://localhost:21148}"

# Aspire AppHost configuration
export Azure__OpenAI__Name="${AZURE_OPENAI_NAME}"
export Azure__OpenAI__ResourceGroup="${AZURE_RESOURCE_GROUP}"
export Azure__AIFoundry__Name="${AZURE_OPENAI_NAME}"
export Azure__AIFoundry__ResourceGroup="${AZURE_RESOURCE_GROUP}"

echo "OTEL_EXPORTER_OTLP_ENDPOINT=$OTEL_EXPORTER_OTLP_ENDPOINT"
echo "AZURE_OPENAI_ENDPOINT=$AZURE_OPENAI_ENDPOINT"
echo "AZURE_OPENAI_DEPLOYMENT_NAME=$AZURE_OPENAI_DEPLOYMENT_NAME"
echo "AZURE_AI_PROJECT_ENDPOINT=$AZURE_AI_PROJECT_ENDPOINT"
echo "AZURE_AI_MODEL_DEPLOYMENT_NAME=$AZURE_AI_MODEL_DEPLOYMENT_NAME"
echo "Azure__OpenAI__Name=$Azure__OpenAI__Name"
echo "Azure__OpenAI__ResourceGroup=$Azure__OpenAI__ResourceGroup"
echo "Azure__AIFoundry__Name=$Azure__AIFoundry__Name"
echo "Azure__AIFoundry__ResourceGroup=$Azure__AIFoundry__ResourceGroup"
