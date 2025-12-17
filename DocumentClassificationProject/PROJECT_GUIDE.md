# Complete Guide: Building an Azure Document Classification & RAG System

This guide walks you through building a complete Retrieval-Augmented Generation (RAG) system for document processing and AI-powered chat using Azure services and Google Gemini AI.

## 📋 Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Understanding](#architecture-understanding)
3. [Step-by-Step Implementation](#step-by-step-implementation)
4. [How Each Component Works](#how-each-component-works)
5. [Testing the System](#testing-the-system)
6. [Deployment](#deployment)

---

## 1. System Overview

### What This System Does

This is a **RAG (Retrieval-Augmented Generation)** pipeline that:
- Accepts document uploads (PDFs, images, etc.)
- Extracts text using Azure Document Intelligence
- Creates vector embeddings using Google Gemini
- Stores documents in Azure AI Search
- Allows users to chat with their documents using AI

### Real-World Use Cases

- **Legal firms**: Search through contracts and case files
- **Healthcare**: Query medical records and research papers
- **Business**: Analyze reports, invoices, and documentation
- **Education**: Create interactive study materials

---

## 2. Architecture Understanding

### The Flow

```
User Upload → Blob Storage → Document Processing → Metadata Storage → Embedding Generation → Vector Index → Chat Interface
```

### Components Explained

#### **Frontend (React)**
- User interface for uploading documents
- Chat interface for asking questions
- Document selector for filtering

#### **Azure Functions (Backend)**
- **BlobTriggerFunction**: Triggers when a file is uploaded
- **DocumentOrchestrator**: Coordinates the entire workflow
- **AnalyzeDocumentActivity**: Extracts text from documents
- **StoreMetadataActivity**: Saves document info to Cosmos DB
- **CreateEmbeddingsActivity**: Generates vector embeddings
- **IndexDocumentActivity**: Stores in Azure AI Search
- **ChatFunction**: Handles user queries and generates answers

#### **Azure Services**
- **Blob Storage**: Stores uploaded files
- **Document Intelligence**: OCR and text extraction
- **Cosmos DB**: NoSQL database for metadata
- **Azure AI Search**: Vector search engine
- **Service Bus**: Message queue for processing

#### **AI Services**
- **Google Gemini API**: Generates embeddings and answers

---

## 3. Step-by-Step Implementation

### Phase 1: Environment Setup (Day 1)

#### Step 1.1: Install Prerequisites

```bash
# Install .NET 8 SDK
sudo snap install dotnet-sdk --classic --channel=8.0

# Verify installation
dotnet --version

# Install Node.js 18+
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Verify
node --version
npm --version

# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Verify
func --version

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure
az login

# Install Azurite (local storage emulator)
npm install -g azurite
```

#### Step 1.2: Get Google Gemini API Key

1. Go to [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the key (you'll need it later)
5. **Free tier**: 15 requests per minute, 1500 per day

---

### Phase 2: Azure Resources (Day 1-2)

#### Step 2.1: Create Resource Group

```bash
# Set variables
RESOURCE_GROUP="rg-doc-classification"
LOCATION="eastus"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION
```

#### Step 2.2: Create Storage Account

```bash
STORAGE_ACCOUNT="docstorage$(date +%s)"  # Unique name

az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Create blob container
az storage container create \
  --name documents \
  --account-name $STORAGE_ACCOUNT

# Get connection string (SAVE THIS!)
az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv
```

#### Step 2.3: Create Cosmos DB

```bash
COSMOS_ACCOUNT="doc-cosmos-$(date +%s)"

# Create Cosmos DB account (takes 5-10 minutes)
az cosmosdb create \
  --name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --locations regionName=$LOCATION

# Create database
az cosmosdb sql database create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --name DocumentClassificationDB

# Create container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --database-name DocumentClassificationDB \
  --name documents \
  --partition-key-path "/id"

# Get connection string (SAVE THIS!)
az cosmosdb keys list \
  --name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv
```

#### Step 2.4: Create Document Intelligence

```bash
DOC_INTELLIGENCE="doc-intelligence-$(date +%s)"

az cognitiveservices account create \
  --name $DOC_INTELLIGENCE \
  --resource-group $RESOURCE_GROUP \
  --kind FormRecognizer \
  --sku S0 \
  --location $LOCATION

# Get endpoint (SAVE THIS!)
az cognitiveservices account show \
  --name $DOC_INTELLIGENCE \
  --resource-group $RESOURCE_GROUP \
  --query properties.endpoint -o tsv

# Get key (SAVE THIS!)
az cognitiveservices account keys list \
  --name $DOC_INTELLIGENCE \
  --resource-group $RESOURCE_GROUP \
  --query key1 -o tsv
```

#### Step 2.5: Create Azure AI Search

```bash
SEARCH_SERVICE="doc-search-$(date +%s)"

az search service create \
  --name $SEARCH_SERVICE \
  --resource-group $RESOURCE_GROUP \
  --sku Basic \
  --location $LOCATION

# Get endpoint (SAVE THIS!)
echo "https://${SEARCH_SERVICE}.search.windows.net"

# Get admin key (SAVE THIS!)
az search admin-key show \
  --service-name $SEARCH_SERVICE \
  --resource-group $RESOURCE_GROUP \
  --query primaryKey -o tsv
```

#### Step 2.6: Create Service Bus

```bash
SERVICE_BUS="doc-servicebus-$(date +%s)"

az servicebus namespace create \
  --name $SERVICE_BUS \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Create queue
az servicebus queue create \
  --name document-queue \
  --namespace-name $SERVICE_BUS \
  --resource-group $RESOURCE_GROUP

# Get connection string (SAVE THIS!)
az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SERVICE_BUS \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv
```

---

### Phase 3: Backend Development (Day 2-4)

#### Step 3.1: Create Azure Functions Project

```bash
# Create project directory
mkdir -p AzureFunctions/DocumentClassification
cd AzureFunctions/DocumentClassification

# Create Functions project
func init . --worker-runtime dotnet-isolated --target-framework net8.0

# Add required NuGet packages
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.DurableTask
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
dotnet add package Azure.AI.FormRecognizer
dotnet add package Microsoft.Azure.Cosmos
dotnet add package Azure.Search.Documents
dotnet add package System.Net.Http.Json
```

#### Step 3.2: Configure local.settings.json

Create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDBConnectionString": "YOUR_COSMOS_CONNECTION_STRING",
    "CosmosDBDatabaseName": "DocumentClassificationDB",
    "CosmosDBContainerName": "documents",
    "DocumentIntelligenceEndpoint": "YOUR_DOC_INTELLIGENCE_ENDPOINT",
    "DocumentIntelligenceKey": "YOUR_DOC_INTELLIGENCE_KEY",
    "SearchServiceEndpoint": "YOUR_SEARCH_ENDPOINT",
    "SearchServiceAdminKey": "YOUR_SEARCH_ADMIN_KEY",
    "GeminiApiKey": "YOUR_GEMINI_API_KEY",
    "ServiceBusConnectionString": "YOUR_SERVICE_BUS_CONNECTION_STRING",
    "DocumentQueueName": "document-queue"
  }
}
```

#### Step 3.3: Create Data Models

Create `Models/DocumentInfo.cs`:

```csharp
namespace DocumentClassification.Models
{
    public class DocumentInfo
    {
        public string BlobUrl { get; set; }
        public string DocumentId { get; set; }
        public string FileName { get; set; }
    }
}
```

Create `Models/DocumentMetadata.cs`:

```csharp
using System;
using Newtonsoft.Json;

namespace DocumentClassification.Models
{
    public class DocumentMetadata
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        public string FileName { get; set; }
        public string BlobUrl { get; set; }
        public string Content { get; set; }
        public DateTime ProcessedDate { get; set; }
        public string Status { get; set; }
    }
}
```

#### Step 3.4: Create Gemini Service

Create `Services/GeminiService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentClassification.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        
        public GeminiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = Environment.GetEnvironmentVariable("GeminiApiKey");
        }

        public async Task<float[]> GenerateEmbeddingsAsync(string text)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_apiKey}";
            
            var request = new
            {
                model = "models/text-embedding-004",
                content = new
                {
                    parts = new[] { new { text = text } }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var embeddingArray = result.RootElement
                .GetProperty("embedding")
                .GetProperty("values");

            var embeddings = new List<float>();
            foreach (var element in embeddingArray.EnumerateArray())
            {
                embeddings.Add(element.GetSingle());
            }

            return embeddings.ToArray();
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return result.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
    }
}
```

#### Step 3.5: Create Activities and Functions

The key functions to create:

1. **BlobTriggerFunction.cs** - Triggers on file upload
2. **DocumentOrchestrator.cs** - Orchestrates the workflow
3. **AnalyzeDocumentActivity.cs** - Extracts text
4. **StoreMetadataActivity.cs** - Saves to Cosmos DB
5. **CreateEmbeddingsActivity.cs** - Generates embeddings
6. **IndexDocumentActivity.cs** - Stores in AI Search
7. **ChatFunction.cs** - Handles chat queries
8. **GetDocumentsFunction.cs** - Lists uploaded documents

(See your existing code for full implementations)

#### Step 3.6: Configure Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DocumentClassification.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddSingleton<GeminiService>();
    })
    .Build();

host.Run();
```

---

### Phase 4: Frontend Development (Day 4-5)

#### Step 4.1: Create React App

```bash
cd ../../
npm create vite@latest frontend -- --template react
cd frontend
npm install
npm install axios
```

#### Step 4.2: Create Components

**src/config.js**:
```javascript
export const API_BASE_URL = 'http://localhost:7071';
```

**src/DocumentUpload.jsx** - File upload component
**src/ChatInterface.jsx** - Chat UI
**src/FileSelector.jsx** - Document filter
**src/App.jsx** - Main application

(See your existing frontend code)

---

### Phase 5: Testing (Day 5-6)

#### Step 5.1: Local Testing

Terminal 1 - Start Azurite:
```bash
mkdir -p ~/azurite-data
azurite --silent --location ~/azurite-data
```

Terminal 2 - Start Backend:
```bash
cd AzureFunctions/DocumentClassification
func start
```

Terminal 3 - Start Frontend:
```bash
cd frontend
npm run dev
```

#### Step 5.2: Upload Test Document

1. Open browser to `http://localhost:5173`
2. Upload a PDF file
3. Check Azure Functions logs for processing
4. Verify in Cosmos DB that metadata is saved
5. Check Azure AI Search for indexed content

#### Step 5.3: Test Chat

1. Type a question about your document
2. Verify the AI responds with relevant information
3. Test document selector filtering

---

## 4. How Each Component Works

### Document Processing Flow

```
1. User uploads file → Blob Storage
2. Blob Trigger fires → Starts Orchestrator
3. Orchestrator calls activities in sequence:
   a. AnalyzeDocument → Extract text
   b. StoreMetadata → Save to Cosmos DB
   c. CreateEmbeddings → Generate vectors
   d. IndexDocument → Store in AI Search
```

### Chat Query Flow

```
1. User asks question → ChatFunction
2. Generate query embedding → Gemini API
3. Vector search → Azure AI Search (finds similar documents)
4. Build context from search results
5. Generate answer → Gemini API with context
6. Return answer to user
```

### Why Vector Embeddings?

Traditional keyword search finds exact matches. Vector embeddings understand **meaning**:

- Query: "What's the total cost?"
- Finds documents mentioning: "price", "amount", "invoice total", etc.

---

## 5. Deployment

### Deploy Backend

```bash
FUNCTION_APP="doc-functions-$(date +%s)"

# Create Function App
az functionapp create \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --storage-account $STORAGE_ACCOUNT

# Configure app settings
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    CosmosDBConnectionString="YOUR_COSMOS_CONNECTION_STRING" \
    DocumentIntelligenceEndpoint="YOUR_DOC_INTELLIGENCE_ENDPOINT" \
    DocumentIntelligenceKey="YOUR_DOC_INTELLIGENCE_KEY" \
    SearchServiceEndpoint="YOUR_SEARCH_ENDPOINT" \
    SearchServiceAdminKey="YOUR_SEARCH_ADMIN_KEY" \
    GeminiApiKey="YOUR_GEMINI_API_KEY" \
    ServiceBusConnectionString="YOUR_SERVICE_BUS_CONNECTION_STRING" \
    DocumentQueueName="document-queue"

# Deploy
cd AzureFunctions/DocumentClassification
func azure functionapp publish $FUNCTION_APP
```

### Deploy Frontend

```bash
cd frontend
npm run build

# Deploy to Azure Static Web Apps, Vercel, or Netlify
```

---

## 6. Key Concepts Explained

### Durable Functions

- **Orchestrator**: Coordinates long-running workflows
- **Activities**: Individual tasks (analyze, store, embed, index)
- **Durable**: State is persisted, can handle failures and retries

### RAG (Retrieval-Augmented Generation)

1. **Retrieval**: Find relevant documents using vector search
2. **Augmentation**: Add found documents as context
3. **Generation**: AI generates answer based on context

### Vector Embeddings

- Text → Array of numbers (e.g., 768 dimensions)
- Similar meanings → Similar vectors
- Enables semantic search

---

## 📝 Summary

This project demonstrates:
- ✅ Serverless architecture with Azure Functions
- ✅ Document processing with AI
- ✅ Vector search and embeddings
- ✅ RAG implementation
- ✅ Modern React frontend
- ✅ Production-ready Azure deployment

**Time to build**: 5-6 days  
**Cost** (Azure): ~$20-50/month (basic tier)  
**Gemini API**: Free tier (1500 requests/day)

---

## 🎯 Next Steps

1. Add user authentication (Azure AD B2C)
2. Implement streaming responses
3. Add multi-language support
4. Create document comparison features
5. Add export functionality
6. Implement rate limiting
7. Add monitoring and analytics

---

**Questions?** Check the main README.md or Azure documentation!
