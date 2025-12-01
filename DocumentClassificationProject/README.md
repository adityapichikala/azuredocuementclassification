# Azure Document Classification System

A serverless document processing system built with Azure Durable Functions, Document Intelligence, Cosmos DB, and Azure OpenAI.

## 🏗️ Architecture

This solution processes documents through an orchestrated workflow:

1. **Document Upload** → HTTP or Service Bus trigger
2. **Document Analysis** → Azure Document Intelligence extracts content
3. **Metadata Storage** → Cosmos DB stores document metadata
4. **Embedding Generation** → Azure OpenAI creates vector embeddings

## 📁 Project Structure

```
DocumentClassificationProject/
├── AzureFunctions/
│   └── DocumentClassification/
│       ├── Models/                    # Data models
│       │   ├── DocumentInfo.cs
│       │   ├── EmbeddedDocument.cs
│       │   └── DocumentMetadata.cs
│       ├── HttpStartFunction.cs       # HTTP trigger
│       ├── ServiceBusStartFunction.cs # Service Bus trigger
│       ├── DocumentOrchestrator.cs    # Main orchestration
│       ├── AnalyzeDocumentActivity.cs # Document Intelligence
│       ├── StoreMetadataActivity.cs   # Cosmos DB storage
│       ├── CreateEmbeddingsActivity.cs # OpenAI embeddings
│       ├── Program.cs                 # Host configuration
│       ├── host.json                  # Functions configuration
│       └── local.settings.json        # Local settings
├── Scripts/
│   └── setup-azure-resources.sh       # Azure resource setup
└── test-request.http                  # HTTP test requests
```

## 🚀 Prerequisites

Before you begin, ensure you have:

- ✅ .NET 8 SDK
- ✅ Azure Functions Core Tools v4
- ✅ Node.js (for Azurite)
- ✅ Azure CLI
- ✅ Active Azure subscription

### Install Prerequisites

```bash
# Install .NET 8 SDK
sudo snap install dotnet-sdk --classic --channel=8.0

# Install Node.js
sudo apt update && sudo apt install -y nodejs npm

# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Install Azurite (local storage emulator)
npm install -g azurite
```

## ⚙️ Setup

### 1. Create Azure Resources

```bash
# Login to Azure
az login

# Run the setup script
cd Scripts
chmod +x setup-azure-resources.sh
./setup-azure-resources.sh
```

The script will create:
- Resource Group
- Storage Account with blob container
- Cosmos DB with database and container
- Document Intelligence resource
- Azure OpenAI resource (requires approval)
- Service Bus namespace with queue

**Important**: Save the output! It contains connection strings you'll need.

### 2. Configure Local Settings

Update `local.settings.json` with the values from the setup script output.

### 3. Build the Project

```bash
cd AzureFunctions/DocumentClassification
dotnet restore
dotnet build
```

## 🧪 Local Development

### Start Azurite

Open a terminal and run:

```bash
mkdir -p ~/azurite-data
azurite --silent --location ~/azurite-data
```

Keep this terminal running.

### Run Functions Locally

Open another terminal:

```bash
cd AzureFunctions/DocumentClassification
func start
```

You should see:

```
Functions:
  DocumentOrchestrator: orchestrationTrigger
  HttpStart: [POST] http://localhost:7071/api/HttpStart
  ServiceBusStart: serviceBusTrigger
  AnalyzeDocumentActivity: activityTrigger
  CreateEmbeddingsActivity: activityTrigger
  StoreMetadataActivity: activityTrigger
```

### Test the Functions

Using cURL:

```bash
curl -X POST http://localhost:7071/api/HttpStart \
  -H "Content-Type: application/json" \
  -d '{
    "blobUrl": "https://YOUR_STORAGE.blob.core.windows.net/documents/test.pdf",
    "documentId": "test-001",
    "fileName": "test.pdf"
  }'
```

Or use the `test-request.http` file with the REST Client extension in VS Code.

## 📊 Monitoring

- Check function logs in the terminal
- View orchestration status via the status URL returned from HttpStart
- Monitor Cosmos DB for stored metadata
- Check Application Insights (if configured)

## 🚢 Deployment

Deploy to Azure:

```bash
# Create Function App
az functionapp create \
  --name YOUR_FUNCTION_APP_NAME \
  --resource-group rg-doc-classification \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --storage-account YOUR_STORAGE_ACCOUNT_NAME

# Deploy
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

## 🔧 Troubleshooting

### Azurite not running
```bash
azurite --silent --location ~/azurite-data
```

### Build errors
```bash
dotnet clean
dotnet restore
dotnet build
```

### Connection issues
- Verify `local.settings.json` has correct connection strings
- Check Azure resources are created and accessible
- Ensure Azurite is running for local development

## 📚 Next Steps

1. ✅ Test with sample PDF documents
2. ✅ Verify metadata in Cosmos DB
3. ✅ Check embeddings are created
4. 🚀 Deploy to Azure
5. 🌐 Create web app for document upload

## 🔗 Resources

- [Azure Durable Functions](https://docs.microsoft.com/azure/azure-functions/durable/)
- [Azure Document Intelligence](https://docs.microsoft.com/azure/applied-ai-services/form-recognizer/)
- [Azure OpenAI](https://docs.microsoft.com/azure/cognitive-services/openai/)
- [Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/)

## 📝 License

This project is for educational and demonstration purposes.
