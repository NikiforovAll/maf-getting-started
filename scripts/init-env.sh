#!/bin/bash
# Set environment variables for MAF samples
# Usage: source scripts/init-env.sh

export AZURE_OPENAI_ENDPOINT="${AZURE_OPENAI_ENDPOINT:-https://oleksii-nikiforov-7668-resource.cognitiveservices.azure.com/}"
export AZURE_OPENAI_DEPLOYMENT_NAME="${AZURE_OPENAI_DEPLOYMENT_NAME:-gpt-4o-mini}"

echo "AZURE_OPENAI_ENDPOINT=$AZURE_OPENAI_ENDPOINT"
echo "AZURE_OPENAI_DEPLOYMENT_NAME=$AZURE_OPENAI_DEPLOYMENT_NAME"
