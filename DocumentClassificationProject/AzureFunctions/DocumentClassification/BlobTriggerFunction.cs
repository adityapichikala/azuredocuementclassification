using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;

namespace DocumentClassification
{
    public class BlobTriggerFunction
    {
        private readonly ILogger<BlobTriggerFunction> _logger;

        public BlobTriggerFunction(ILogger<BlobTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobTriggerFunction))]
        public async Task Run(
            [BlobTrigger("documents/{name}", Connection = "StorageConnectionString")] Stream stream,
            string name,
            [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation($"📂 Blob trigger processing file: {name}");

            var supportedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".heif", ".docx", ".xlsx", ".pptx", ".html" };
            var extension = Path.GetExtension(name).ToLowerInvariant();

            if (!supportedExtensions.Contains(extension))
            {
                _logger.LogWarning($"⚠️ Skipping unsupported file type: {name}");
                return;
            }

            // Generate a unique instance ID based on the file name and timestamp
            // Using a hash or clean name to ensure it's a valid ID
            string instanceId = $"auto-{Guid.NewGuid().ToString("N")}";
            
            // Construct the blob URL
            // Note: In a real scenario, we might want to get this from binding data or configuration
            // For now, we construct it using the known storage account format
            string accountName = "stdocclass1764130250"; // We could also parse this from connection string
            string blobUrl = $"https://{accountName}.blob.core.windows.net/documents/{Uri.EscapeDataString(name)}";

            var documentInfo = new DocumentInfo
            {
                DocumentId = instanceId,
                FileName = name,
                BlobUrl = blobUrl
            };

            var options = new StartOrchestrationOptions { InstanceId = instanceId };

            await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(DocumentOrchestrator),
                documentInfo,
                options);

            _logger.LogInformation($"🚀 Started orchestration with ID = '{instanceId}' for file '{name}'");
        }
    }
}
