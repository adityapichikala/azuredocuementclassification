#!/bin/bash

# Azure Document Classification - Resource Setup Script
# This script creates all required Azure resources

set -e

echo "🚀 Starting Azure Resource Setup..."

# Variables
RESOURCE_GROUP="rg-doc-classification"
LOCATION="centralindia"
STORAGE_ACCOUNT="stdocclass$(date +%s)"
COSMOSDB_ACCOUNT="cosmos-doc-class-$(date +%s)"
DOC_INTELLIGENCE="di-doc-class-$(date +%s)"
OPENAI_ACCOUNT="openai-doc-class-$(date +%s)"
SERVICE_BUS="sb-doc-class-$(date +%s)"

echo "📋 Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo ""

# Create Resource Group
echo "📦 Creating Resource Group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Storage Account
echo "💾 Creating Storage Account..."
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Get Storage Connection String
STORAGE_CONNECTION=$(az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv)

# Create Blob Container
echo "📁 Creating Blob Container..."
az storage container create \
  --name documents \
  --connection-string "$STORAGE_CONNECTION"

# Create Cosmos DB Account
echo "🌐 Creating Cosmos DB Account..."
az cosmosdb create \
  --name $COSMOSDB_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --locations regionName=$LOCATION

# Create Cosmos DB Database
echo "🗄️ Creating Cosmos DB Database..."
az cosmosdb sql database create \
  --account-name $COSMOSDB_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --name DocumentMetadata

# Create Cosmos DB Container
echo "📊 Creating Cosmos DB Container..."
az cosmosdb sql container create \
  --account-name $COSMOSDB_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --database-name DocumentMetadata \
  --name Documents \
  --partition-key-path "/documentId"

# Get Cosmos DB Connection String
COSMOS_CONNECTION=$(az cosmosdb keys list \
  --name $COSMOSDB_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" \
  --output tsv)

# Create Document Intelligence
echo "📄 Creating Document Intelligence..."
az cognitiveservices account create \
  --name $DOC_INTELLIGENCE \
  --resource-group $RESOURCE_GROUP \
  --kind FormRecognizer \
  --sku S0 \
  --location $LOCATION \
  --yes

# Get Document Intelligence Endpoint and Key
DOC_INTEL_ENDPOINT=$(az cognitiveservices account show \
  --name $DOC_INTELLIGENCE \
  --resource-group $RESOURCE_GROUP \
  --query properties.endpoint \
  --output tsv)

DOC_INTEL_KEY=$(az cognitiveservices account keys list \
  --name $DOC_INTELLIGENCE \
  --resource-group $RESOURCE_GROUP \
  --query key1 \
  --output tsv)

# Create Azure OpenAI (Note: This requires special approval)
echo "🤖 Creating Azure OpenAI..."
echo "⚠️  Note: Azure OpenAI requires special approval. This may fail if not approved."
az cognitiveservices account create \
  --name $OPENAI_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --kind OpenAI \
  --sku S0 \
  --location swedencentral \
  --yes || echo "⚠️  Azure OpenAI creation failed. You may need to apply for access."

# Get OpenAI Endpoint and Key (if created)
OPENAI_ENDPOINT=$(az cognitiveservices account show \
  --name $OPENAI_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query properties.endpoint \
  --output tsv 2>/dev/null || echo "")

OPENAI_KEY=$(az cognitiveservices account keys list \
  --name $OPENAI_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query key1 \
  --output tsv 2>/dev/null || echo "")

# Deploy Embedding Model (if OpenAI was created)
if [ ! -z "$OPENAI_ENDPOINT" ]; then
  echo "🚀 Deploying Embedding Model..."
  az cognitiveservices account deployment create \
    --name $OPENAI_ACCOUNT \
    --resource-group $RESOURCE_GROUP \
    --deployment-name text-embedding-ada-002 \
    --model-name text-embedding-ada-002 \
    --model-version "2" \
    --model-format OpenAI \
    --sku-capacity 1 \
    --sku-name "Standard"
fi

# Create Service Bus Namespace
echo "🚌 Creating Service Bus Namespace..."
az servicebus namespace create \
  --name $SERVICE_BUS \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard

# Create Service Bus Queue
echo "📬 Creating Service Bus Queue..."
az servicebus queue create \
  --name document-queue \
  --namespace-name $SERVICE_BUS \
  --resource-group $RESOURCE_GROUP

# Get Service Bus Connection String
SERVICE_BUS_CONNECTION=$(az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SERVICE_BUS \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv)

# Output Configuration
echo ""
echo "✅ Azure Resources Created Successfully!"
echo ""
echo "📝 Update your local.settings.json with these values:"
echo ""
echo "{
  \"IsEncrypted\": false,
  \"Values\": {
    \"AzureWebJobsStorage\": \"UseDevelopmentStorage=true\",
    \"FUNCTIONS_WORKER_RUNTIME\": \"dotnet-isolated\",
    \"DocumentIntelligenceEndpoint\": \"$DOC_INTEL_ENDPOINT\",
    \"DocumentIntelligenceKey\": \"$DOC_INTEL_KEY\",
    \"CosmosDBConnection\": \"$COSMOS_CONNECTION\",
    \"OpenAIEndpoint\": \"$OPENAI_ENDPOINT\",
    \"OpenAIKey\": \"$OPENAI_KEY\",
    \"OpenAIEmbeddingModel\": \"text-embedding-ada-002\",
    \"ServiceBusConnection\": \"$SERVICE_BUS_CONNECTION\"
  }
}"

echo ""
echo "💡 Save this output! You'll need these values for your Functions app."
