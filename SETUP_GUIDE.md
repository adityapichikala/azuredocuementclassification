# Setup Guide - Azure Document Classification Project

This guide will help you set up the Azure Document Classification project on any machine (Windows, Linux, or macOS).

## Table of Contents
- [Prerequisites](#prerequisites)
- [Initial Setup](#initial-setup)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

1. **Node.js** (v18 or higher)
   - Download: https://nodejs.org/
   - Verify installation: `node --version`

2. **.NET SDK** (v8.0 or higher)
   - Download: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

3. **Azure Functions Core Tools** (v4)
   ```bash
   npm install -g azure-functions-core-tools@4
   ```
   - Verify installation: `func --version`

4. **Git**
   - Windows: https://git-scm.com/download/win
   - Linux: `sudo apt install git` (Ubuntu/Debian)
   - Verify installation: `git --version`

5. **Python** (v3.8+) - Optional, for debug scripts
   - Download: https://www.python.org/downloads/
   - Verify installation: `python --version` or `python3 --version`

### Azure Account & Resources

You need an active Azure subscription with the following resources already created:
- Azure Storage Account
- Azure Cosmos DB
- Azure Document Intelligence
- Azure AI Search
- Google Gemini API Key (free tier)

> **Note**: If you're moving from one machine to another with the same Azure account, your resources are already set up in the cloud. You just need the connection strings/keys.

---

## Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/adityapichikala/azuredocuementclassification.git
cd azuredocuementclassification
```

### 2. Project Structure

```
DocumentClassificationProject/
├── AzureFunctions/
│   └── DocumentClassification/     # Backend Azure Functions
│       ├── local.settings.json     # ⚠️ YOU NEED TO CREATE THIS
│       └── ...
├── frontend/                        # React Frontend
│   ├── .env                         # ⚠️ YOU NEED TO CREATE THIS
│   └── ...
├── PROJECT_GUIDE.md                 # Project documentation
└── README.md                        # Project overview
```

---

## Configuration

### 1. Backend Configuration

Create `local.settings.json` in the Azure Functions directory:

**Path**: `DocumentClassificationProject/AzureFunctions/DocumentClassification/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=YOUR_STORAGE_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDBConnection": "AccountEndpoint=https://YOUR_COSMOS_ACCOUNT.documents.azure.com:443/;AccountKey=YOUR_COSMOS_KEY;",
    "DocumentIntelligenceEndpoint": "https://YOUR_DOC_INTELLIGENCE.cognitiveservices.azure.com/",
    "DocumentIntelligenceKey": "YOUR_DOC_INTELLIGENCE_KEY",
    "AzureAISearchEndpoint": "https://YOUR_SEARCH_SERVICE.search.windows.net",
    "AzureAISearchKey": "YOUR_SEARCH_ADMIN_KEY",
    "GeminiApiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

> **Important**: This file is gitignored for security. You must create it manually on each machine.

#### How to Get Your Azure Connection Strings

1. **Azure Portal**: https://portal.azure.com
2. **Storage Account**:
   - Navigate to your Storage Account → Access Keys
   - Copy the connection string (key1 or key2)
   
3. **Cosmos DB**:
   - Navigate to your Cosmos DB Account → Keys
   - Copy the PRIMARY CONNECTION STRING
   
4. **Document Intelligence**:
   - Navigate to your Document Intelligence resource → Keys and Endpoint
   - Copy the KEY and ENDPOINT
   
5. **Azure AI Search**:
   - Navigate to your Search Service → Keys
   - Copy the Primary admin key and URL
   
6. **Gemini API**:
   - Visit: https://aistudio.google.com/app/apikey
   - Generate/copy your API key

### 2. Frontend Configuration

Create `.env` file in the frontend directory:

**Path**: `DocumentClassificationProject/frontend/.env`

```env
VITE_API_BASE_URL=http://localhost:7071/api
```

> **Note**: This points to your local backend. When deploying to production, update this to your deployed Azure Functions URL.

---

## Running the Application

### 1. Install Dependencies

#### Backend (Azure Functions)

```bash
cd DocumentClassificationProject/AzureFunctions/DocumentClassification
dotnet restore
```

#### Frontend (React)

```bash
cd DocumentClassificationProject/frontend
npm install
```

### 2. Start the Application

You need **two separate terminal windows/tabs**:

#### Terminal 1: Start Backend

```bash
cd DocumentClassificationProject/AzureFunctions/DocumentClassification
func start
```

**Expected Output:**
```
Azure Functions Core Tools
...
Functions:
  UploadFunction: [POST] http://localhost:7071/api/upload
  ChatFunction: [POST] http://localhost:7071/api/chat
  ...
```

> The backend will run on **http://localhost:7071**

#### Terminal 2: Start Frontend

```bash
cd DocumentClassificationProject/frontend
npm run dev
```

**Expected Output:**
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
```

> The frontend will run on **http://localhost:5173**

### 3. Access the Application

Open your browser and navigate to:
```
http://localhost:5173
```

---

## Platform-Specific Notes

### Windows

- Use **PowerShell** or **Windows Terminal** (recommended)
- Python command: `python` (not `python3`)
- Paths use backslashes `\` but Node/npm handle this automatically
- If you get execution policy errors with PowerShell:
  ```powershell
  Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted
  ```

### Linux (Ubuntu/Debian/Zorin)

- Use **bash** or **zsh** terminal
- Python command: `python3`
- Install Python dependencies if running debug scripts:
  ```bash
  pip3 install azure-storage-blob
  ```

### macOS

- Use **Terminal** or **iTerm2**
- Python command: `python3`
- Install Python dependencies if running debug scripts:
  ```bash
  pip3 install azure-storage-blob
  ```

---

## Troubleshooting

### Common Issues

#### 1. Port Already in Use

**Error**: `Port 7071 is already in use`

**Solution**: Kill the process using the port or change the port:
- Windows: 
  ```powershell
  netstat -ano | findstr :7071
  taskkill /PID <PID> /F
  ```
- Linux/Mac:
  ```bash
  lsof -ti:7071 | xargs kill -9
  ```

#### 2. Azure Functions Not Starting

**Error**: `Missing value for AzureWebJobsStorage` or similar

**Solution**: Verify your `local.settings.json` file exists and contains all required values.

#### 3. Frontend Can't Connect to Backend

**Error**: Network errors or CORS issues

**Solution**:
1. Ensure backend is running on port 7071
2. Check `.env` file has correct `VITE_API_BASE_URL`
3. Restart both frontend and backend

#### 4. .NET SDK Not Found

**Error**: `The command 'dotnet' is not recognized`

**Solution**:
1. Download and install .NET SDK: https://dotnet.microsoft.com/download
2. Restart your terminal/command prompt
3. Verify: `dotnet --version`

#### 5. NPM Packages Won't Install

**Error**: Permission denied or network errors

**Solution**:
- Windows: Run terminal as Administrator
- Linux/Mac: Use `sudo npm install -g` for global packages
- Try clearing npm cache: `npm cache clean --force`

#### 6. Azure Storage Connection Issues

**Error**: Authentication failures or 403 errors

**Solution**:
1. Verify your Storage Account connection string is correct
2. Check if your Storage Account allows access from your IP
3. Ensure the storage account key hasn't been regenerated
4. In Azure Portal → Storage Account → Networking → Ensure "Enabled from all networks" or add your IP

#### 7. Cosmos DB Connection Issues

**Error**: Connection timeout or authentication errors

**Solution**:
1. Verify Cosmos DB connection string in `local.settings.json`
2. Check Cosmos DB firewall settings in Azure Portal
3. Azure Portal → Cosmos DB → Networking → Add your IP or enable "All networks"

---

## Running Debug Scripts (Optional)

### Debug Blobs Script

This script lists all containers and blobs in your storage account.

**Prerequisites**:
```bash
pip install azure-storage-blob
# or
pip3 install azure-storage-blob
```

**Set environment variable**:

Windows (PowerShell):
```powershell
$env:AzureWebJobsStorage="your-connection-string"
python DocumentClassificationProject/debug_blobs.py
```

Linux/Mac:
```bash
export AzureWebJobsStorage="your-connection-string"
python3 DocumentClassificationProject/debug_blobs.py
```

---

## Deployment to Azure (Optional)

If you want to deploy the Azure Functions to Azure (not just run locally):

1. **Login to Azure**:
   ```bash
   az login
   ```

2. **Deploy Functions**:
   ```bash
   cd DocumentClassificationProject/AzureFunctions/DocumentClassification
   func azure functionapp publish YOUR_FUNCTION_APP_NAME
   ```

3. **Update Frontend `.env`**:
   ```env
   VITE_API_BASE_URL=https://YOUR_FUNCTION_APP_NAME.azurewebsites.net/api
   ```

4. **Build and Deploy Frontend**:
   - You can deploy to Azure Static Web Apps, Netlify, Vercel, etc.

---

## Additional Resources

- **Azure Portal**: https://portal.azure.com
- **Project Documentation**: See `PROJECT_GUIDE.md` for architecture and implementation details
- **Azure Functions Documentation**: https://learn.microsoft.com/azure/azure-functions/
- **React Documentation**: https://react.dev/
- **Azure Storage Documentation**: https://learn.microsoft.com/azure/storage/

---

## Support

If you encounter issues not covered in this guide:

1. Check the Azure Portal for service status
2. Review logs in:
   - Backend: Terminal where `func start` is running
   - Frontend: Browser Developer Console (F12)
   - Azure Portal: Function App → Monitor → Logs
3. Verify all Azure resources are in the same region for better performance
4. Ensure your Azure subscription has sufficient credits/quota

---

## Security Best Practices

1. **Never commit** `local.settings.json` or `.env` files to Git
2. **Rotate your keys** periodically in the Azure Portal
3. **Use Azure Key Vault** for production deployments
4. **Restrict network access** to your Azure resources when possible
5. **Monitor usage** to avoid unexpected costs

---

*Last Updated: December 29, 2025*
