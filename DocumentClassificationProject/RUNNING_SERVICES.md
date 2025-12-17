# 🚀 Running Services - Session Started

**Date**: December 4, 2025 at 14:40 IST

---

## ✅ All Services Running

### 1. Azurite (Storage Emulator)
- **Status**: ✅ Running
- **Blob Service**: http://127.0.0.1:10000
- **Queue Service**: http://127.0.0.1:10001
- **Table Service**: http://127.0.0.1:10002
- **Purpose**: Emulates Azure Storage locally for development

### 2. Azure Functions Backend
- **Status**: ✅ Running
- **Base URL**: http://localhost:7071
- **Purpose**: Backend API and document processing

**Available Endpoints**:
```
POST   http://localhost:7071/api/Upload
POST   http://localhost:7071/api/Chat
GET    http://localhost:7071/api/GetDocuments
```

**Running Functions**:
- ✅ BlobTriggerFunction (automatically processes uploads)
- ✅ DocumentOrchestrator (coordinates workflow)
- ✅ AnalyzeDocumentActivity
- ✅ StoreMetadataActivity
- ✅ IndexDocumentActivity
- ✅ ChatFunction
- ✅ GetDocumentsFunction
- ✅ UploadFunction

### 3. React Frontend
- **Status**: ✅ Running
- **URL**: http://localhost:5173
- **Purpose**: User interface for document upload and chat

---

## 🎯 Quick Actions

### Upload a Document
**Via Frontend**:
1. Open http://localhost:5173
2. Click "Choose File"
3. Select a PDF or document
4. Click "Upload"

**Via curl**:
```bash
curl -X POST http://localhost:7071/api/Upload \
  -F "file=@/path/to/your/document.pdf"
```

### Chat with Documents
**Via Frontend**:
1. Open http://localhost:5173
2. Type your question in the chat box
3. Click "Send"

**Via curl**:
```bash
curl -X POST http://localhost:7071/api/Chat \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What is this document about?",
    "fileNames": []
  }'
```

### Get List of Documents
```bash
curl http://localhost:7071/api/GetDocuments
```

---

## 📊 Service Health Check

### Check Azurite
```bash
curl http://127.0.0.1:10000/devstoreaccount1?comp=list
```

### Check Azure Functions
```bash
curl http://localhost:7071/api/GetDocuments
```

### Check Frontend
Open: http://localhost:5173

---

## 🛑 How to Stop Services

### Stop All Services
```bash
# Find all running processes
ps aux | grep -E "azurite|func|vite"

# Kill Azurite
pkill azurite

# Kill Azure Functions
pkill -f "func start"

# Kill Frontend
pkill -f "vite"
```

### Or Use Ctrl+C
- Press `Ctrl+C` in each terminal window where services are running

---

## 🔄 How to Restart Services

If you need to restart:

### 1. Start Azurite
```bash
mkdir -p ~/azurite-data
azurite --silent --location ~/azurite-data
```

### 2. Start Backend
```bash
cd /home/aditya/Desktop/azure/DocumentClassificationProject/AzureFunctions/DocumentClassification
dotnet build && func start
```

### 3. Start Frontend
```bash
cd /home/aditya/Desktop/azure/DocumentClassificationProject/frontend
npm run dev
```

---

## 📝 Current Logs

### Backend Logs
Check the terminal running Azure Functions for:
- Document processing status
- API request/response logs
- Error messages

### Frontend Logs
Check browser console (F12) for:
- API calls
- Component rendering
- JavaScript errors

---

## 🎉 Ready to Use!

Your complete RAG system is now running:

1. **Upload documents** → http://localhost:5173
2. **Documents are processed** automatically
3. **Chat with your documents** → http://localhost:5173

**System Flow**:
```
Upload → Blob Storage → Process → Extract Text → Generate Embeddings → Index → Chat Ready
```

---

**For help, see**: [IMPLEMENTATION_GUIDE.md](./IMPLEMENTATION_GUIDE.md)
