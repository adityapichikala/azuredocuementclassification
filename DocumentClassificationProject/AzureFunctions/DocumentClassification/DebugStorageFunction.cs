using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DocumentClassification
{
    public class DebugStorageFunction
    {
        private readonly ILogger _logger;

        public DebugStorageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DebugStorageFunction>();
        }

        [Function("DebugStorage")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Debugging storage structure...");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var writer = new StreamWriter(response.Body);

            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                await writer.WriteLineAsync("Error: StorageConnectionString is missing.");
                await writer.FlushAsync();
                return response;
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                await writer.WriteLineAsync("--- Containers & Blobs ---");

                await foreach (var container in blobServiceClient.GetBlobContainersAsync())
                {
                    await writer.WriteLineAsync($"Container: [{container.Name}]");
                    
                    var containerClient = blobServiceClient.GetBlobContainerClient(container.Name);
                    int count = 0;
                    await foreach (var blob in containerClient.GetBlobsAsync())
                    {
                        // List ALL blobs for training-data, otherwise limit to 10
                        if (container.Name == "training-data" || count < 10) 
                        {
                            await writer.WriteLineAsync($"  - {blob.Name}");
                        }
                        count++;
                    }
                    if (container.Name != "training-data" && count >= 10) await writer.WriteLineAsync($"  ... (Total: {count} blobs)");
                    await writer.WriteLineAsync("");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"Error accessing storage: {ex.Message}");
            }

            await writer.FlushAsync();
            return response;
        }
    }
}
