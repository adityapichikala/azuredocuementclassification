using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Text.Json;

namespace DocumentClassification
{
    public class TrainClassifierFunction
    {
        private readonly ILogger _logger;

        public TrainClassifierFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TrainClassifierFunction>();
        }

        [Function("TrainClassifier")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Starting model training request.");

            // 1. Parse Request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TrainRequest? data = null;
            try
            {
                data = JsonSerializer.Deserialize<TrainRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Ignore
            }

            if (data == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body.");
                return badRequest;
            }

            string modelId = data.ModelId ?? $"classifier-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            string containerName = data.ContainerName ?? "training-data";

            // 2. Get Configuration
            var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndpoint");
            var key = Environment.GetEnvironmentVariable("DocumentIntelligenceKey");
            var storageConnection = Environment.GetEnvironmentVariable("StorageConnectionString");

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(storageConnection))
            {
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Missing configuration (Endpoint, Key, or Storage).");
                return error;
            }

            try
            {
                // 3. Generate SAS URI for the training container
                // var sasUri = await GenerateContainerSasUri(storageConnection, containerName);
                // _logger.LogInformation($"Generated SAS URI: {sasUri}");
                
                // 4. Train Model
                // 4. Train Model
                var credential = new AzureKeyCredential(key);
                var client = new DocumentModelAdministrationClient(new Uri(endpoint), credential);

                _logger.LogInformation($"Training model '{modelId}' using data from '{containerName}'...");

                var blobServiceClient = new BlobServiceClient(storageConnection);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                
                if (!await containerClient.ExistsAsync())
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteStringAsync($"Container '{containerName}' not found.");
                    return notFound;
                }

                var docTypes = new Dictionary<string, ClassifierDocumentTypeDetails>();
                
                var folders = new HashSet<string>();
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    var parts = blob.Name.Split('/');
                    if (parts.Length > 1)
                    {
                        folders.Add(parts[0]);
                    }
                }

                if (folders.Count < 2)
                {
                    var badData = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badData.WriteStringAsync($"Found {folders.Count} categories. Need at least 2 categories (folders) in '{containerName}' to train a classifier.");
                    return badData;
                }

                // Hardcoded SAS for debugging
                var sasToken = "?se=2025-12-22T00%3A00%3A00Z&sp=rl&sv=2022-11-02&sr=c&sig=9cy1vcE%2BLRrqjaaPTCtoWoVmle%2B3nsHk5qDdMbBBjmY%3D";
                var sasUri = new Uri($"https://stdocclass1764130250.blob.core.windows.net/{containerName}{sasToken}");
                _logger.LogInformation($"Using Hardcoded SAS URI: {sasUri}");

                foreach (var folder in folders)
                {
                    var source = new BlobContentSource(sasUri)
                    {
                        Prefix = folder 
                    };
                    
                    docTypes.Add(folder, new ClassifierDocumentTypeDetails(source));
                    _logger.LogInformation($"Added category: {folder}");
                }

                // Start Training
                Operation<DocumentClassifierDetails> operation = await client.BuildDocumentClassifierAsync(WaitUntil.Completed, docTypes, modelId);
                DocumentClassifierDetails classifier = operation.Value;

                // 5. Return Success
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new 
                { 
                    message = "Training complete!",
                    modelId = classifier.ClassifierId,
                    createdOn = classifier.CreatedOn,
                    description = classifier.Description,
                    docTypes = classifier.DocumentTypes
                });
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Training failed: {ex.Message}");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Training failed: {ex.Message}");
                return error;
            }
        }

        private async Task<Uri> GenerateContainerSasUri(string connectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (containerClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    Resource = "c", // c for container
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List);

                return containerClient.GenerateSasUri(sasBuilder);
            }
            
            throw new InvalidOperationException("Cannot generate SAS URI for this account.");
        }

        public class TrainRequest
        {
            public string? ModelId { get; set; }
            public string? ContainerName { get; set; }
        }
    }
}
