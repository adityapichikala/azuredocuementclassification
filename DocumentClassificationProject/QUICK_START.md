# 🚀 QUICK START GUIDE

## When You Return - Run These Commands:

### 1️⃣ Terminal 1: Start Azurite
```bash
mkdir -p ~/azurite-data
azurite --silent --location ~/azurite-data
```
**Keep this terminal running!**

---

### 2️⃣ Terminal 2: Run Functions
```bash
cd /home/aditya/Desktop/azure/DocumentClassificationProject/AzureFunctions/DocumentClassification
dotnet restore
dotnet build
func start
```

---

### 3️⃣ Terminal 3: Test the System

#### Upload a test PDF to Azure:
```bash
# Replace /path/to/test.pdf with your actual PDF file
az storage blob upload \
  --account-name stdocclass1764130250 \
  --container-name documents \
  --name test.pdf \
  --file /path/to/test.pdf \
  --auth-mode key
```

#### Trigger the orchestration:
```bash
curl -X POST http://localhost:7071/api/HttpStart \
  -H "Content-Type: application/json" \
  -d '{
    "blobUrl": "https://stdocclass1764130250.blob.core.windows.net/documents/test.pdf",
    "documentId": "test-001",
    "fileName": "test.pdf"
  }'
```

---

## ⚠️ Important Notes:

1. **local.settings.json** is already configured ✅
2. **Azure OpenAI** is NOT set up (optional - apply at https://aka.ms/oai/access)
3. **All other Azure resources** are ready ✅

---

## 📁 Important Files:

- **Full Progress:** `/home/aditya/Desktop/azure/DocumentClassificationProject/PROGRESS.md`
- **Project README:** `/home/aditya/Desktop/azure/DocumentClassificationProject/README.md`
- **Configuration:** `/home/aditya/Desktop/azure/DocumentClassificationProject/AzureFunctions/DocumentClassification/local.settings.json`

---

## 💰 Cost Warning:

Resources are running and incurring costs! When done testing:

```bash
# Delete all resources to stop costs
az group delete --name rg-doc-classification --yes
```

---

## 🆘 Need Help?

Check `PROGRESS.md` for detailed troubleshooting and next steps!
