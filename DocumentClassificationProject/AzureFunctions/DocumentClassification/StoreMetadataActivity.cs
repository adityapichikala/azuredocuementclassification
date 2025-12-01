using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DocumentClassification;

public class StoreMetadataActivity
{
    private readonly ILogger<StoreMetadataActivity> _logger;
    private static CosmosClient? _cosmosClient;

    public StoreMetadataActivity(ILogger<StoreMetadataActivity> logger)
    {
        _logger = logger;
    }

    [Function(nameof(StoreMetadataActivity))]
    public async Task Run([ActivityTrigger] EmbeddedDocument document)
    {
        _logger.LogInformation($"💾 Storing metadata for: {document.CorrelationId}");

        var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Cosmos DB connection string not configured");
        }

        try
        {
            if (_cosmosClient == null)
            {
                var clientOptions = new CosmosClientOptions
                {
                    RequestTimeout = TimeSpan.FromSeconds(60),
                    OpenTcpConnectionTimeout = TimeSpan.FromSeconds(120),
                    MaxRetryAttemptsOnRateLimitedRequests = 9,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
                };
                
                _cosmosClient = new CosmosClient(connectionString, clientOptions);
                _logger.LogInformation("🔌 Created new Cosmos DB client with extended timeouts");
            }
            
            var container = _cosmosClient.GetContainer("DocumentMetadata", "Documents");

            var metadata = new DocumentMetadata
            {
                Id = document.CorrelationId,
                DocumentId = document.DocumentId,
                DocumentType = document.DocumentType,
                StartPage = document.StartPage,
                EndPage = document.EndPage,
                UploadDate = DateTime.UtcNow,
                Content = document.Content
            };

            await container.CreateItemAsync(metadata, new PartitionKey(document.DocumentId));

            _logger.LogInformation($"✅ Metadata stored successfully for {document.CorrelationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error storing metadata: {ex.Message}");
            throw;
        }
    }
}
