using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DocumentClassification.Services;

namespace DocumentClassification;

public class IndexDocumentActivity
{
    private readonly ILogger<IndexDocumentActivity> _logger;
    private readonly GeminiService _geminiService;

    public IndexDocumentActivity(ILogger<IndexDocumentActivity> logger, GeminiService geminiService)
    {
        _logger = logger;
        _geminiService = geminiService;
    }

    [Function(nameof(IndexDocumentActivity))]
    public async Task Run([ActivityTrigger] DocumentMetadata metadata)
    {
        _logger.LogInformation($"🔍 Indexing document: {metadata.DocumentId}");

        var searchEndpoint = Environment.GetEnvironmentVariable("SearchServiceEndpoint");
        var searchKey = Environment.GetEnvironmentVariable("SearchServiceAdminKey");
        var indexName = "documents-index";

        if (string.IsNullOrEmpty(searchEndpoint) || string.IsNullOrEmpty(searchKey))
        {
            _logger.LogWarning("⚠️ Azure AI Search credentials not configured - skipping indexing");
            return;
        }

        try
        {
            var credential = new AzureKeyCredential(searchKey);
            var indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
            var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

            // Create index if it doesn't exist
            await CreateIndexIfNotExists(indexClient, indexName);

            // Generate embeddings
            float[]? embeddings = null;
            try 
            {
                if (!string.IsNullOrEmpty(metadata.Content))
                {
                    // Truncate to avoid token limits (Gemini embedding-001 has a limit, safe bet is ~8000 chars or less)
                    var textToEmbed = metadata.Content.Length > 8000 ? metadata.Content.Substring(0, 8000) : metadata.Content;
                    embeddings = await _geminiService.GenerateEmbeddingsAsync(textToEmbed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"⚠️ Failed to generate embeddings for {metadata.DocumentId}: {ex.Message}");
                // Continue indexing without embeddings if it fails
            }

            // Create the search document
            var searchDocument = new SearchDocument
            {
                ["id"] = metadata.DocumentId,
                ["documentId"] = metadata.DocumentId,
                ["fileName"] = metadata.FileName,
                ["blobUrl"] = metadata.BlobUrl,
                ["documentType"] = metadata.DocumentType,
                ["content"] = metadata.Content,
                ["uploadDate"] = metadata.UploadDate
            };

            if (embeddings != null)
            {
                searchDocument["contentVector"] = embeddings;
            }

            // Upload to index
            await searchClient.MergeOrUploadDocumentsAsync(new[] { searchDocument });

            _logger.LogInformation($"✅ Document indexed successfully: {metadata.DocumentId} (Embeddings: {embeddings != null})");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error indexing document: {ex.Message}");
            // We don't throw here to allow the orchestration to complete even if indexing fails
        }
    }

    private async Task CreateIndexIfNotExists(SearchIndexClient indexClient, string indexName)
    {
        try
        {
            await indexClient.GetIndexAsync(indexName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation($"Creating new search index: {indexName}");

            var definition = new SearchIndex(indexName)
            {
                Fields = new List<SearchField>
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SimpleField("documentId", SearchFieldDataType.String) { IsFilterable = true },
                    new SearchableField("fileName") { IsFilterable = true, IsSortable = true },
                    new SimpleField("blobUrl", SearchFieldDataType.String),
                    new SimpleField("documentType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                    new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.EnLucene },
                    new SimpleField("uploadDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    
                    // Vector field
                    new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 768, // Gemini embedding-001 dimensions
                        VectorSearchProfileName = "my-vector-profile"
                    }
                },
                VectorSearch = new VectorSearch
                {
                    Profiles = { new VectorSearchProfile("my-vector-profile", "my-hnsw-config") },
                    Algorithms = { new HnswAlgorithmConfiguration("my-hnsw-config") }
                }
            };

            await indexClient.CreateIndexAsync(definition);
        }
    }
}
