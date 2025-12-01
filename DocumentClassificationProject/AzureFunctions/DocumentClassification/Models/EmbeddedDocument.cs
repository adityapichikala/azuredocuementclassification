namespace DocumentClassification;

public class EmbeddedDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int StartPage { get; set; }
    public int EndPage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public DateTimeOffset UploadDate { get; set; }
}
