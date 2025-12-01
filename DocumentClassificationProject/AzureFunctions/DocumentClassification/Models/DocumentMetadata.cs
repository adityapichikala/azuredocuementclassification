using System.Text.Json.Serialization;

namespace DocumentClassification;

public class DocumentMetadata
{
    [JsonPropertyName("id")]
    [Newtonsoft.Json.JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("documentId")]
    [Newtonsoft.Json.JsonProperty("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    [Newtonsoft.Json.JsonProperty("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("blobUrl")]
    [Newtonsoft.Json.JsonProperty("blobUrl")]
    public string BlobUrl { get; set; } = string.Empty;

    [JsonPropertyName("documentType")]
    [Newtonsoft.Json.JsonProperty("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("startPage")]
    [Newtonsoft.Json.JsonProperty("startPage")]
    public int StartPage { get; set; }

    [JsonPropertyName("endPage")]
    [Newtonsoft.Json.JsonProperty("endPage")]
    public int EndPage { get; set; }

    [JsonPropertyName("timestamp")]
    [Newtonsoft.Json.JsonProperty("timestamp")]
    public DateTime UploadDate { get; set; }

    [JsonPropertyName("content")]
    [Newtonsoft.Json.JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}
