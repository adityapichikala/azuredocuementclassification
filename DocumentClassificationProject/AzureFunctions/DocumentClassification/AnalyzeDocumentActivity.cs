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
        _logger.LogInformation($"📄 Processing document: {documentInfo.FileName}");

        try 
        {
            string content = "";
            int pageCount = 0;
            string documentType = "Unknown";
            
            var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndpoint");
            var key = Environment.GetEnvironmentVariable("DocumentIntelligenceKey");
            var classifierId = Environment.GetEnvironmentVariable("ClassifierModelId");

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
                throw new InvalidOperationException("Configuration missing.");

            var credential = new AzureKeyCredential(key);
            var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            // Generate SAS for API access
            Uri fileUri = await GenerateBlobSasUri(documentInfo.BlobUrl);

            // 1. CLASSIFY
            if (!string.IsNullOrEmpty(classifierId))
            {
                try 
                {
                    _logger.LogInformation($"🕵️ Classifying with {classifierId}...");
                    var op = await client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, classifierId, fileUri);
                    if (op.Value.Documents.Count > 0)
                    {
                        documentType = op.Value.Documents[0].DocumentType;
                        _logger.LogInformation($"✅ Classified as: {documentType}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Classification failed: {ex.Message}");
                }
            }

            // 2. EXTRACT (RAG)
            // Using 'prebuilt-layout' to get text from ANY document type
            _logger.LogInformation("📖 Extracting text...");
            var extractOp = await client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, "prebuilt-layout", fileUri);
            content = extractOp.Value.Content;
            pageCount = extractOp.Value.Pages.Count;

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
            _logger.LogError($"❌ Error: {ex.Message}");
            throw;
        }
    }

    private async Task<Uri> GenerateBlobSasUri(string blobUrl)
    {
        try
        {
            var blobUri = new Uri(blobUrl);
            var storageConnection = Environment.GetEnvironmentVariable("StorageConnectionString");
            if (string.IsNullOrEmpty(storageConnection)) return blobUri;

            var blobServiceClient = new BlobServiceClient(storageConnection);
            var containerName = blobUri.Segments[1].TrimEnd('/');
            var blobName = string.Join("", blobUri.Segments.Skip(2));
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            if (blobClient.CanGenerateSasUri)
            {
                var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1))
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b"
                };
                return blobClient.GenerateSasUri(sas);
            }
            return blobUri;
        }
        catch { return new Uri(blobUrl); }
    }
}
