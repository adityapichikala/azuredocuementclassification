# Azure Document Classification & Chat System

**An AI-powered invoice processing system using Azure Document Intelligence, with optional RAG-based conversational insights**

This project is an **AI-driven invoice processing and analysis system** that automates
invoice ingestion, data extraction, classification, and querying.

At its core, the system uses **Azure Document Intelligence (Prebuilt Invoice Model)** to
extract structured invoice data.  
On top of this structured data, an optional **RAG-based conversational layer**
allows users to ask natural-language questions across invoices.

---

## ❓ The Problem & Solution

### The Problem
Organizations are drowning in documents.
- **Manual Entry**: Extracting data from invoices (Vendor, Date, Total) is tedious and error-prone.
- **Hard to Find**: Traditional search requires exact keywords. If you search for "Computer Repair", you won't find an invoice that only says "Laptop Service".
- **No Insights**: A folder full of PDFs is a "black box". You can't easily query it for insights like "How much did we spend on software this year?".

### The Solution
We automate invoice understanding in three layers:

1. **Invoice Understanding (Core Layer)**
   - Detect invoices automatically
   - Extract structured fields (Vendor, Date, Total, Line Items)
   - Classify documents as "Invoice" using AI confidence

2. **Data Storage & Analytics (Operational Layer)**
   - Store extracted invoice metadata in Cosmos DB
   - Enable fast filtering, reporting, and dashboards

3. **Conversational Insights (Advanced Layer – RAG)**
   - Convert invoice text into embeddings
   - Enable semantic search and chat-based querying


### 💡 Real-World Example
**Scenario**: You upload a PDF invoice from "Acme Corp" for "10x Widgets" costing "$500".
1.  **System Action**: It extracts `Vendor: Acme Corp`, `Total: $500`. It also indexes the text.
2.  **User Query**: "How much did we pay for the widgets?"
3.  **System Response**: "You paid **$500** for widgets to Acme Corp." (It found the answer even though you didn't ask for the specific invoice number).

---

## 🌟 Features

- **📂 Auto-Classification**: Automatically categorizes uploaded files (e.g., "Contract", "Invoice", "Resume") using a custom-trained Azure Document Intelligence model.
- **📄 Universal Text Extraction**: Uses the `prebuilt-layout` model to extract high-quality text and tables from *any* document type for the AI chat.
- **💬 AI Chat Interface**: Interactive chat with your documents using Google Gemini AI (RAG pattern).
- **🔍 Vector Search**: Semantic search powered by Azure AI Search embeddings.
- **📊 Metadata Storage**: Classifications and metadata stored in Azure Cosmos DB for filtering and organization.
- **🚀 Serverless Architecture**: Scalable Azure Durable Functions orchestration.

---

## 🏗️ Architecture

### Primary Architecture – Invoice Intelligence Pipeline

1. Invoice uploaded to Blob Storage
2. Azure Data Factory / Blob Trigger initiates processing
3. Azure Document Intelligence (Prebuilt Invoice Model)
4. Extracted invoice metadata stored in Cosmos DB

### Optional RAG Extension

5. Text chunks converted to embeddings
6. Indexed in Azure AI Search
7. Gemini generates contextual responses

This solution implements a "Classify-Then-Extract" pipeline:

1.  **Ingestion** → User uploads file to `documents` container.
2.  **Orchestration** → `BlobTrigger` starts the Durable Function.
3.  **Analysis (Dual-Pass)**:
    * **Step 1:** The **Classifier Model** identifies the document type (e.g., "Contract").
    * **Step 2:** The **Layout Model** extracts all text and structure for the Chat AI.
4.  **Embedding** → Google Gemini API creates vector embeddings of the text.
5.  **Indexing** → Azure AI Search stores the vector and the "Document Type" tag.
6.  **Chat** → Users query documents; the system filters by category and answers using Gemini.

graph TD
    A[User Uploads Invoice] --> B[Azure Blob Storage]
    B --> C[Blob Trigger Function]
    C --> D[Document Orchestrator (Durable)]
    
    D --> E[Analyze Document Activity]
    E --> F[Azure Document Intelligence (Prebuilt Invoice Model)]
    F --> D
    
    D --> G[Store Metadata Activity]
    G --> H[Azure Cosmos DB]
    H --> D
    
    D --> I[Create Embeddings Activity]
    I --> J[Gemini API (Text Embeddings)]
    J --> D
    
    D --> K[Index Document Activity]
    K --> L[Azure AI Search]
    L --> D
    
    M[User Query] --> N[Chat Function]
    N --> O[Generate Query Embedding]
    O --> L
    L --> P[Retrieve Relevant Docs]
    P --> N
    N --> Q[Gemini API (Generate Answer)]
    Q --> R[Return Response]


## ☁️ Azure Services Explained

We use a specific set of Azure services, each chosen for a distinct purpose:

### 1. Azure Functions (The "Workers")
-   **What it is**: Serverless compute service. You run code without managing servers.
-   **Usage**: It hosts our backend logic. We use **Durable Functions** to define a workflow (Analysis -> Embedding -> Indexing) that is resilient. If one step fails, we can retry it without restarting the whole process.

### 2. Azure Blob Storage (The "Hard Drive")
-   **What it is**: Massively scalable object storage for files.
-   **Usage**: This is where the raw PDF and Image files are physically stored. We use a container named `documents`.

### 3. Azure Document Intelligence (The "Eyes")
-   **What it is**: AI service that applies machine learning to extract text, key-value pairs, and tables from documents.
-   **Usage**: We use the **Prebuilt Invoice Model**. It "looks" at the document and identifies: *"This text is the Vendor Name"*, *"This text is the Total Amount"*. It handles the OCR (Optical Character Recognition).

### 4. Azure Cosmos DB (The "Filing Cabinet")
-   **What it is**: A fast, NoSQL database.
-   **Usage**: We store the **Metadata** here. While the file is in Blob Storage, the *information about the file* (ID, Name, Upload Date, Extracted Fields) lives here as a JSON document. This allows the frontend to quickly list all files without scanning the heavy blobs.

### 5. Azure AI Search (The "Brain")
-   **What it is**: A search-as-a-service solution with vector search capabilities.
-   **Usage**: This is our search engine. It stores the **Vector Embeddings**. When you ask a question, it performs a "Vector Search" (Nearest Neighbor search) to find the most relevant document segments, even if the words don't match exactly.

---

## 🤖 Document Intelligence & Machine Learning

### Why Prebuilt Invoice Model?
- Eliminates manual training effort
- Trained on millions of real-world invoices
- High accuracy across layouts
- Ideal for enterprise invoice automation

### How the Model Works
We utilize the **Azure Document Intelligence Prebuilt Invoice Model**.
-   **Training**: This model was pre-trained by Microsoft on **millions of invoices** from various regions and industries. It has learned to recognize common invoice layouts, terminology (e.g., "Total", "Amount Due", "Balance"), and structures.
-   **You do NOT need to train it**: It works out-of-the-box.
-   **Classification**:
    -   The system sends the document to the model.
    -   The model attempts to map the content to its known schema (Vendor, Customer, Total, etc.).
    -   **Confidence Score**: The model returns a confidence score (0-1).
    -   **Our Logic**: If the model successfully extracts valid invoice fields (like `InvoiceTotal`), our code classifies the document type as **"invoice"**. If it fails to find these fields, we mark it as **"Unknown"**.

### Vector Embeddings (The "Semantic" Layer)
-   We use **Google Gemini (`text-embedding-004`)** to turn text into numbers.
-   **Example**: The words "Dog" and "Puppy" are different words but have similar meanings. In the vector space, their numbers will be very close together. This allows the chat to understand context.

---

## ⚙️ Azure Account Setup & Configuration

To replicate this environment, you need an Azure Subscription. Here is how the account is structured:

### 1. Resource Group
-   **Name**: `rg-doc-classification` (example)
-   **Purpose**: A logical container that holds all related resources. If you delete this group, you delete everything (clean up is easy).

### 2. Service Configuration
Each service is connected via **Connection Strings** or **Keys** stored in the Function App's settings (`local.settings.json` for local, Environment Variables for cloud).

| Service | Key Setting | Description |
| :--- | :--- | :--- |
| **Storage Account** | `AzureWebJobsStorage` | Used by Azure Functions to manage its own state. |
| **Cosmos DB** | `CosmosDBConnectionString` | Allows the code to write metadata JSONs. |
| **Doc Intelligence** | `DocumentIntelligenceEndpoint` | The URL where we send files for analysis. |
| **Doc Intelligence** | `DocumentIntelligenceKey` | The password to access the AI service. |
| **AI Search** | `SearchServiceEndpoint` | The URL of our search index. |
| **AI Search** | `SearchServiceAdminKey` | Admin key to allow creating/updating indexes. |
| **Google Gemini** | `GeminiApiKey` | Key to generate embeddings and chat responses. |

---

## 📁 Project Structure

```
DocumentClassificationProject/
├── AzureFunctions/
│   └── DocumentClassification/
│       ├── AnalyzeDocumentActivity.cs      # Core logic: Classifies type & extracts text for RAG
│       ├── BlobTriggerFunction.cs          # Detects new uploads in Blob Storage
│       ├── ChatFunction.cs                 # RAG endpoint: Semantic search + Gemini answer
│       ├── DeleteDocumentFunction.cs       # Removes docs from Storage, Cosmos, and Search
│       ├── DocumentOrchestrator.cs         # Durable Function: Manages the processing workflow
│       ├── GetDocumentsFunction.cs         # API to fetch classified metadata from Cosmos DB
│       ├── IndexDocumentActivity.cs        # Generates embeddings and updates AI Search index
│       ├── ReindexFunction.cs              # Utility to refresh the search index
│       ├── StoreMetadataActivity.cs        # Saves classification and text to Cosmos DB
│       ├── TrainClassifierFunction.cs      # API to train your custom classification model
│       ├── UploadFunction.cs               # Direct API upload endpoint
│       ├── Program.cs                      # Dependency injection and host setup
│       ├── host.json                       # Azure Functions runtime configuration
│       ├── local.settings.json             # App settings (Keys, Endpoints, Model IDs)
│       ├── Models/                         # Data transfer objects
│       │   ├── DocumentInfo.cs             # Trigger input model
│       │   ├── DocumentMetadata.cs         # Cosmos DB schema
│       │   └── EmbeddedDocument.cs         # Search index schema
│       └── Services/
│           └── GeminiService.cs            # Google Gemini API integration (Embeddings/Chat)
├── frontend/                                # React + Vite Frontend
│   ├── src/
│   │   ├── components/
│   │   │   ├── ui/                         # Reusable UI components (Button, Card)
│   │   │   ├── Layout.jsx                  # Main application wrapper
│   │   │   └── PdfViewer.jsx               # Document preview component
│   │   ├── App.jsx                         # Main dashboard logic
│   │   ├── ChatInterface.jsx               # RAG chat UI
│   │   ├── FileUpload.jsx                  # Document ingestion component
│   │   └── main.jsx                        # React entry point
│   ├── tailwind.config.js                  # Styling configuration
│   └── package.json                        # Frontend dependencies
├── Scripts/
│   └── setup-azure-resources.sh            # CLI script to deploy all Azure infra
└── README.md                               # Project documentation and setup guide
```

## 🚀 Prerequisites

Before you begin, ensure you have:

- ✅ .NET 8 SDK
- ✅ Azure Functions Core Tools v4
- ✅ Node.js 18+ and npm
- ✅ Azure CLI
- ✅ Active Azure subscription
- ✅ Google Gemini API key (free tier available)

### Install Prerequisites

```bash
# Install .NET 8 SDK
sudo snap install dotnet-sdk --classic --channel=8.0

# Install Node.js (Ubuntu/Debian)
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Install Azurite (local storage emulator)
npm install -g azurite
```

## ⚙️ Setup

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd DocumentClassificationProject
```

### 2. Create Azure Resources

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
- Azure AI Search service
- Service Bus namespace with queue

**Important**: Save the output! It contains connection strings you'll need.

### 3. Get Gemini API Key

1. Visit [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Create a new API key
3. Save it for the next step

### 4. Configure Backend Settings

Update `AzureFunctions/DocumentClassification/local.settings.json`:

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

### 5. Build Backend

```bash
cd AzureFunctions/DocumentClassification
dotnet restore
dotnet build
```

### 6. Setup Frontend

```bash
cd frontend
npm install
```

Update `frontend/src/config.js` with your Function App URL (use `http://localhost:7071` for local development).

## 🧪 Local Development

### Start Azurite (Terminal 1)

```bash
mkdir -p ~/azurite-data
azurite --silent --location ~/azurite-data
```

Keep this terminal running.

### Run Azure Functions (Terminal 2)

```bash
cd AzureFunctions/DocumentClassification
func start
```

You should see:

```
Functions:
  BlobTriggerFunction: blobTrigger
  DocumentOrchestrator: orchestrationTrigger
  Chat: [POST] http://localhost:7071/api/Chat
  GetDocuments: [GET] http://localhost:7071/api/GetDocuments
  AnalyzeDocumentActivity: activityTrigger
  CreateEmbeddingsActivity: activityTrigger
  IndexDocumentActivity: activityTrigger
  StoreMetadataActivity: activityTrigger
```

### Run Frontend (Terminal 3)

```bash
cd frontend
npm run dev
```

Open your browser to `http://localhost:5173`

## 📊 Using the Application

### Invoice Processing
1. Upload invoice (PDF/Image)
2. System extracts structured invoice data
3. Metadata stored and available instantly

### Conversational Queries (Optional)
- Ask invoice-related questions
- Perform semantic spend analysis


### Example Queries

```
"What is the main topic of the invoice?"
"Summarize the key points from the contract"
"What is the total amount mentioned?"
```

## 🔧 API Endpoints

### Upload Document (via Blob Storage)
Upload files to the `documents` container in your Storage Account

### Chat
```http
POST /api/Chat
Content-Type: application/json

{
  "query": "Your question here",
  "fileNames": ["document1.pdf", "document2.pdf"]  // Optional
}
```

### Get Documents
```http
GET /api/GetDocuments
```

## 🚢 Deployment

### Deploy Backend to Azure

```bash
# Create Function App
az functionapp create \
  --name YOUR_FUNCTION_APP_NAME \
  --resource-group rg-doc-classification \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --storage-account YOUR_STORAGE_ACCOUNT_NAME

# Configure app settings
az functionapp config appsettings set \
  --name YOUR_FUNCTION_APP_NAME \
  --resource-group rg-doc-classification \
  --settings \
    CosmosDBConnectionString="YOUR_COSMOS_CONNECTION_STRING" \
    DocumentIntelligenceEndpoint="YOUR_DOC_INTELLIGENCE_ENDPOINT" \
    # ... add all other settings

# Deploy
cd AzureFunctions/DocumentClassification
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

### Deploy Frontend

You can deploy the frontend to:
- Azure Static Web Apps
- Vercel
- Netlify
- Azure App Service

```bash
cd frontend
npm run build
# Upload the 'dist' folder to your hosting service
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
- Verify CORS settings if frontend can't connect

### Document processing issues
- Check function logs for errors
- Verify Document Intelligence can access the blob URL
- Ensure the document format is supported (PDF, JPEG, PNG, etc.)

### Chat not working
- Verify Gemini API key is valid
- Check Azure AI Search index exists and has documents
- Review Chat function logs for errors
- Ensure embeddings were created successfully

## 📈 Monitoring

- **Function Logs**: Check terminal output or Azure Portal
- **Cosmos DB**: Query the `documents` container
- **Azure AI Search**: Use Search Explorer in Azure Portal
- **Application Insights**: Enable for production monitoring

## 🔒 Security Notes

- Store secrets in Azure Key Vault for production
- Use Managed Identity instead of connection strings
- Enable authentication on Function App
- Implement CORS properly for production
- Rotate API keys regularly

## 📚 Technologies Used

- **Backend**: .NET 8, Azure Durable Functions
- **Frontend**: React 18, Vite
- **AI/ML**: Google Gemini API (Gemini 1.5 Flash for chat, Text Embedding 004)
- **Search**: Azure AI Search (vector search)
- **Storage**: Azure Blob Storage, Cosmos DB
- **Document Processing**: Azure Document Intelligence
- **Messaging**: Azure Service Bus

## 🗺️ Roadmap

- [x] Add support for more document formats (Images/Photos)
- [ ] Implement user authentication
- [ ] Add document versioning
- [ ] Create document comparison feature
- [ ] Add export chat history
- [ ] Implement streaming responses
- [ ] Add multi-language support

## 🎯 Project Focus Summary

| Layer | Purpose |
|------|--------|
| Document Intelligence | Core invoice understanding |
| Cosmos DB | Structured invoice storage |
| RAG (Search + Gemini) | Advanced analytics & chat |


## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is for educational and demonstration purposes.

## 🔗 Resources

- [Azure Durable Functions](https://docs.microsoft.com/azure/azure-functions/durable/)
- [Azure Document Intelligence](https://docs.microsoft.com/azure/applied-ai-services/form-recognizer/)
- [Azure AI Search](https://docs.microsoft.com/azure/search/)
- [Google Gemini API](https://ai.google.dev/)
- [Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/)

---

Made with ❤️ using Azure and Google Gemini AI
