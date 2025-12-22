using System.Net;
using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DocumentClassification
{
    public class DeleteDocumentFunction
    {
        private readonly ILogger _logger;
        private static CosmosClient? _cosmosClient;

        public DeleteDocumentFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteDocumentFunction>();
        }

        [Function("DeleteDocument")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("🗑️ DeleteDocument function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<DeleteRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.DocumentId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Please pass documentId in the request body");
                return badResponse;
            }

            try
            {
                // 1. Delete from Cosmos DB
                await DeleteFromCosmosDB(data.DocumentId);

                // 2. Delete from Blob Storage (only if URL is provided)
                if (!string.IsNullOrEmpty(data.BlobUrl))
                {
                    await DeleteFromBlobStorage(data.BlobUrl);
                }

                // 3. Delete from AI Search
                await DeleteFromSearchIndex(data.DocumentId);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("Document deleted successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error deleting document: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error deleting document: {ex.Message}");
                return errorResponse;
            }
        }

        private async Task DeleteFromCosmosDB(string documentId)
        {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
            if (string.IsNullOrEmpty(connectionString)) return;

            if (_cosmosClient == null)
            {
                _cosmosClient = new CosmosClient(connectionString);
            }

            var container = _cosmosClient.GetContainer("DocumentMetadata", "Documents");

            try
            {
                // The item's 'id' might be different from 'documentId' (e.g. CorrelationId).
                // Since 'documentId' is the PartitionKey, we can efficiently query for the item.
                var query = new QueryDefinition("SELECT * FROM c WHERE c.documentId = @documentId")
                    .WithParameter("@documentId", documentId);

                using var iterator = container.GetItemQueryIterator<dynamic>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(documentId) });

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var item = response.FirstOrDefault();

                    if (item != null)
                    {
                        string id = item.id;
                        await container.DeleteItemAsync<object>(id, new PartitionKey(documentId));
                        _logger.LogInformation($"✅ Deleted metadata for {documentId} (id: {id}) from Cosmos DB");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Document {documentId} not found in Cosmos DB (query returned no results)");
                    }
                }
            }
            catch (CosmosException ex)
            {
                _logger.LogError($"❌ Error deleting from Cosmos DB: {ex.Message}");
            }
        }

        private async Task DeleteFromBlobStorage(string blobUrl)
        {
            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            if (string.IsNullOrEmpty(connectionString)) return;

            try
            {
                var blobUri = new Uri(blobUrl);
                var containerName = blobUri.Segments[1].TrimEnd('/');
                var blobName = string.Join("", blobUri.Segments.Skip(2));

                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation($"✅ Deleted blob {blobName} from storage");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error deleting blob: {ex.Message}");
                throw;
            }
        }

        private async Task DeleteFromSearchIndex(string documentId)
        {
            var searchEndpoint = Environment.GetEnvironmentVariable("SearchServiceEndpoint");
            var searchKey = Environment.GetEnvironmentVariable("SearchServiceAdminKey");

            if (string.IsNullOrEmpty(searchEndpoint) || string.IsNullOrEmpty(searchKey)) return;

            try
            {
                var credential = new AzureKeyCredential(searchKey);
                var client = new SearchClient(new Uri(searchEndpoint), "documents-index", credential);

                // The key field in our index is 'id', which corresponds to the Cosmos DB 'id' (which is our documentId)
                // Wait, in StoreMetadataActivity: Id = document.CorrelationId, DocumentId = document.DocumentId
                // Let's check IndexDocumentActivity to see what 'id' is mapped to.
                // In IndexDocumentActivity: Id = document.DocumentId.
                // So we should delete by DocumentId.

                var actions = new[] { IndexDocumentsAction.Delete("id", documentId) };
                // Note: The 'Delete' action requires a key-value pair where the key is the key field name and value is the key value.
                // However, the SDK helper `Delete` usually takes the document object or a key-value bag.
                // Let's use the simpler overload if possible or construct a dynamic object.
                
                await client.DeleteDocumentsAsync("id", new[] { documentId });
                _logger.LogInformation($"✅ Deleted document {documentId} from AI Search index");
            }
            catch (Exception ex)
            {
                 _logger.LogWarning($"⚠️ Error deleting from search index (might not exist): {ex.Message}");
            }
        }

        public class DeleteRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("documentId")]
            public string DocumentId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("blobUrl")]
            public string BlobUrl { get; set; }
        }
    }
}
