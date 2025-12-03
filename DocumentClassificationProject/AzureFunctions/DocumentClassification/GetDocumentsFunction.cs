using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentClassification
{
    public class GetDocumentsFunction
    {
        private readonly ILogger _logger;
        private static CosmosClient? _cosmosClient;

        public GetDocumentsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetDocumentsFunction>();
        }

        [Function("GetDocuments")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Fetching document list.");

            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Cosmos DB connection string not configured.");
                return error;
            }

            try
            {
                if (_cosmosClient == null)
                {
                    _cosmosClient = new CosmosClient(connectionString);
                }

                var container = _cosmosClient.GetContainer("DocumentMetadata", "Documents");
                var query = new QueryDefinition("SELECT c.fileName, c.documentId, c.timestamp FROM c");
                
                var documents = new List<DocumentSummary>();
                using var iterator = container.GetItemQueryIterator<DocumentSummary>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    documents.AddRange(response);
                }

                var responseData = req.CreateResponse(HttpStatusCode.OK);
                await responseData.WriteAsJsonAsync(documents);
                return responseData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching documents: {ex.Message}");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error fetching documents: {ex.Message}");
                return error;
            }
        }

        public class DocumentSummary
        {
            [JsonPropertyName("fileName")]
            public string FileName { get; set; } = string.Empty;

            [JsonPropertyName("documentId")]
            public string DocumentId { get; set; } = string.Empty;

            [JsonPropertyName("timestamp")]
            public DateTime? UploadDate { get; set; }
        }
    }
}
