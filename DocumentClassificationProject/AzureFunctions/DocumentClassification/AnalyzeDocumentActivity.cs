using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DocumentClassification;

public class AnalyzeDocumentActivity
{
    private readonly ILogger<AnalyzeDocumentActivity> _logger;

    public AnalyzeDocumentActivity(ILogger<AnalyzeDocumentActivity> logger)
    {
        _logger = logger;
    }

    [Function(nameof(AnalyzeDocumentActivity))]
    public async Task<DocumentMetadata> Run([ActivityTrigger] DocumentInfo documentInfo)
    {
        _logger.LogInformation($"📄 Analyzing document: {documentInfo.FileName}");

        try 
        {
            string content = "";
            int pageCount = 1;
            string documentType = "Unknown"; // Default
            bool azureClassificationSuccess = false;

            if (documentInfo.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                // Handle text files directly
                _logger.LogInformation("📝 Detected text file, downloading content directly...");
                var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
                if (string.IsNullOrEmpty(storageConnectionString)) throw new InvalidOperationException("StorageConnectionString not configured");

                var blobUri = new Uri(documentInfo.BlobUrl);
                var containerName = blobUri.Segments[1].TrimEnd('/');
                var blobName = string.Join("", blobUri.Segments.Skip(2));

                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var downloadResult = await blobClient.DownloadContentAsync();
                content = downloadResult.Value.Content.ToString();
            }
            else
            {
                // Use Document Intelligence for other formats
                var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndpoint");
                var key = Environment.GetEnvironmentVariable("DocumentIntelligenceKey");
                var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(storageConnectionString))
                {
                    throw new InvalidOperationException("Configuration missing (Endpoint, Key, or StorageConnection)");
                }

                var credential = new AzureKeyCredential(key);
                var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

                // Download blob content to stream
                var blobUri = new Uri(documentInfo.BlobUrl);
                var containerName = blobUri.Segments[1].TrimEnd('/');
                var blobName = string.Join("", blobUri.Segments.Skip(2));
                
                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                using var blobStream = await blobClient.OpenReadAsync();

                // 1. Extract Content using Prebuilt Invoice Model
                _logger.LogInformation("🧾 Using Azure Prebuilt Invoice model...");
                var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", blobStream);
                var result = operation.Value;
                
                content = result.Content;
                pageCount = result.Pages.Count;

                // Extract structured invoice data
                var invoiceData = new System.Text.StringBuilder();
                invoiceData.AppendLine("\n\n--- 🧾 EXTRACTED INVOICE DATA ---");
                
                foreach (var document in result.Documents)
                {
                    documentType = "invoice"; // Force type to invoice
                    azureClassificationSuccess = true; // Mark as classified

                    if (document.Fields.TryGetValue("VendorName", out var vendorName) && vendorName.Value.AsString() != null)
                        invoiceData.AppendLine($"Vendor: {vendorName.Value.AsString()}");
                    
                    if (document.Fields.TryGetValue("CustomerName", out var customerName) && customerName.Value.AsString() != null)
                        invoiceData.AppendLine($"Customer: {customerName.Value.AsString()}");

                    if (document.Fields.TryGetValue("InvoiceId", out var invoiceId) && invoiceId.Value.AsString() != null)
                        invoiceData.AppendLine($"Invoice ID: {invoiceId.Value.AsString()}");

                    if (document.Fields.TryGetValue("InvoiceDate", out var invoiceDate))
                        invoiceData.AppendLine($"Date: {invoiceDate.Value.AsDate():yyyy-MM-dd}");

                    if (document.Fields.TryGetValue("InvoiceTotal", out var invoiceTotal))
                    {
                        var currency = invoiceTotal.Value.AsCurrency();
                        invoiceData.AppendLine($"Total: {currency.Amount} {currency.Symbol}");
                    }
                }
                
                // Append structured data to content for LLM context
                content += invoiceData.ToString();
            }

            // --- INVOICE ONLY SCOPE ---
            // We rely on the prebuilt-invoice model. 
            // If extraction succeeded, documentType is already set to 'invoice'.
            // If not, it remains 'Unknown'.
            
            if (documentType == "Unknown")
            {
                _logger.LogWarning("⚠️ Document could not be classified as an invoice.");
            }

            var preview = content?.Length > 200 ? content.Substring(0, 200) + "..." : content ?? "";
            _logger.LogInformation($"✅ Analysis complete. Pages: {pageCount}, Type: {documentType}, Content Length: {content?.Length ?? 0}");

            return new DocumentMetadata
            {
                Id = documentInfo.DocumentId,
                DocumentId = documentInfo.DocumentId,
                FileName = documentInfo.FileName,
                BlobUrl = documentInfo.BlobUrl,
                DocumentType = documentType,
                StartPage = 1,
                EndPage = pageCount,
                UploadDate = DateTime.UtcNow,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error analyzing document: {ex.Message}");
            throw;
        }
    }

    private async Task<Uri> GenerateBlobSasUri(string blobUrl)
    {
        try
        {
            var blobUri = new Uri(blobUrl);
            
            // Extract container and blob name from URL
            var segments = blobUri.Segments;
            if (segments.Length < 3)
            {
                return blobUri;
            }
            
            var containerName = segments[1].TrimEnd('/');
            var blobName = string.Join("", segments.Skip(2));
            
            var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                return blobUri;
            }
            
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                
                return blobClient.GenerateSasUri(sasBuilder);
            }
            
            return blobUri;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"⚠️ Failed to generate SAS token: {ex.Message}. Using original URL.");
            return new Uri(blobUrl);
        }
    }
}
