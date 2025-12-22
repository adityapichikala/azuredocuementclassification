using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.DurableTask.Client;

namespace DocumentClassification
{
    public class ReindexFunction
    {
        private readonly ILogger _logger;

        public ReindexFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ReindexFunction>();
        }

        [Function("Reindex")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("Starting reindexing of all documents...");

            string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            string containerName = "documents";
            
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            string filename = req.Query["filename"];

            int count = 0;
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (!string.IsNullOrEmpty(filename) && !blobItem.Name.Equals(filename, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string blobUrl = containerClient.GetBlobClient(blobItem.Name).Uri.ToString();
                
                var documentInfo = new DocumentInfo
                {
                    DocumentId = Guid.NewGuid().ToString(), // Generate new ID for re-indexing
                    FileName = blobItem.Name,
                    BlobUrl = blobUrl
                };

                string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                    nameof(DocumentOrchestrator),
                    documentInfo);

                _logger.LogInformation($"Started orchestration {instanceId} for {blobItem.Name}");
                count++;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Started reindexing for {count} documents.");
            return response;
        }
    }
}
