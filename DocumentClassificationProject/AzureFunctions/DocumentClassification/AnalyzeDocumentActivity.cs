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
                _logger.LogInformation($"✅ Downloaded text content. Length: {content.Length}");
            }
            else
            {
                // Use Document Intelligence for other formats
                var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndpoint");
                var key = Environment.GetEnvironmentVariable("DocumentIntelligenceKey");

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException("Document Intelligence credentials not configured");
                }

                var credential = new AzureKeyCredential(key);
                var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

                // Generate SAS URI for the blob so Document Intelligence can access it
                var sasUri = await GenerateBlobSasUri(documentInfo.BlobUrl);

                // Analyze the document
                var operation = await client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, "prebuilt-read", sasUri);
                var result = operation.Value;
                
                content = result.Content;
                pageCount = result.Pages.Count;
                _logger.LogInformation($"✅ Analysis complete. Pages: {pageCount}, Content Length: {content.Length}");
            }

            return new DocumentMetadata
            {
                Id = documentInfo.DocumentId,
                DocumentId = documentInfo.DocumentId,
                FileName = documentInfo.FileName,
                BlobUrl = documentInfo.BlobUrl,
                DocumentType = Path.GetExtension(documentInfo.FileName).TrimStart('.').ToUpperInvariant(),
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
