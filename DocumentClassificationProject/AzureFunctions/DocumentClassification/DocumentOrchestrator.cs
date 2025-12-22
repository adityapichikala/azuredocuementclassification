using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DocumentClassification;

public class DocumentOrchestrator
{
    private readonly ILogger<DocumentOrchestrator> _logger;

    public DocumentOrchestrator(ILogger<DocumentOrchestrator> logger)
    {
        _logger = logger;
    }

    [Function(nameof(DocumentOrchestrator))]
    public async Task<List<EmbeddedDocument>> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var documentInfo = context.GetInput<DocumentInfo>();
        
        if (!context.IsReplaying)
        {
            _logger.LogInformation($"🎯 Orchestrating document: {documentInfo?.FileName}");
        }

        // 1. Analyze Document (Extract Text)
        var metadata = await context.CallActivityAsync<DocumentMetadata>(
            nameof(AnalyzeDocumentActivity),
            documentInfo);

        if (!context.IsReplaying)
        {
            _logger.LogInformation($"📄 Analyzed document: {metadata.FileName} (Content Length: {metadata.Content?.Length ?? 0})");
        }

        // 2. Index Document (Generate Embeddings & Upload to Search)
        if (!context.IsReplaying) _logger.LogInformation($"🔍 Calling IndexDocumentActivity for {metadata.DocumentId}...");
        await context.CallActivityAsync(
            nameof(IndexDocumentActivity),
            metadata);
        if (!context.IsReplaying) _logger.LogInformation($"✅ IndexDocumentActivity completed for {metadata.DocumentId}");

        // 3. Store Metadata (Cosmos DB)
        var embeddedDoc = new EmbeddedDocument
        {
            CorrelationId = metadata.Id,
            DocumentId = metadata.DocumentId,
            Content = metadata.Content,
            DocumentType = metadata.DocumentType,
            FileName = metadata.FileName,
            BlobUrl = metadata.BlobUrl,
            UploadDate = metadata.UploadDate
        };

        if (!context.IsReplaying) _logger.LogInformation($"💾 Calling StoreMetadataActivity for {metadata.DocumentId}...");
        await context.CallActivityAsync(
            nameof(StoreMetadataActivity),
            embeddedDoc);

        if (!context.IsReplaying)
        {
            _logger.LogInformation($"✅ Document processing complete: {documentInfo?.FileName}");
        }

        return new List<EmbeddedDocument> { embeddedDoc };
    }
}
