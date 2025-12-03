using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure;
using DocumentClassification.Services;
using System.Text.Json;

namespace DocumentClassification
{
    public class ChatFunction
    {
        private readonly ILogger _logger;
        private readonly GeminiService _geminiService;

        public ChatFunction(ILoggerFactory loggerFactory, GeminiService geminiService)
        {
            _logger = loggerFactory.CreateLogger<ChatFunction>();
            _geminiService = geminiService;
        }

        [Function("Chat")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing chat request.");

            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ChatRequest? data = null;
            try 
            {
                data = JsonSerializer.Deserialize<ChatRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Ignore deserialization errors
            }

            string? query = data?.Query;

            if (string.IsNullOrEmpty(query))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Please pass a query in the request body.");
                return badRequest;
            }

            try 
            {
                // 1. Generate embedding for the query
                var queryEmbedding = await _geminiService.GenerateEmbeddingsAsync(query);

            // 2. Search Azure AI Search
                var searchEndpoint = Environment.GetEnvironmentVariable("SearchServiceEndpoint");
                var searchKey = Environment.GetEnvironmentVariable("SearchServiceAdminKey");
                var indexName = "documents-index";
                
                if (string.IsNullOrEmpty(searchEndpoint) || string.IsNullOrEmpty(searchKey))
                {
                     var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                     await errorResponse.WriteStringAsync("Search service not configured.");
                     return errorResponse;
                }

                var credential = new AzureKeyCredential(searchKey);
                var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

                var searchOptions = new SearchOptions
                {
                    VectorSearch = new VectorSearchOptions
                    {
                        Queries = { new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = 3, Fields = { "contentVector" } } }
                    },
                    Size = 3, // Top 3 documents
                    Select = { "content", "fileName" }
                };

                // Apply filter if specific files are selected
                if (data.FileNames != null && data.FileNames.Count > 0)
                {
                    // Escape filenames to prevent injection (basic) and handle commas
                    var escapedFileNames = data.FileNames.Select(f => f.Replace("'", "''"));
                    // Construct search.in filter: search.in(fileName, 'file1.pdf,file2.txt', ',')
                    string fileList = string.Join(",", escapedFileNames);
                    searchOptions.Filter = $"search.in(fileName, '{fileList}', ',')";
                    _logger.LogInformation($"Applying filter: {searchOptions.Filter}");
                }

                var searchResults = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);

                var context = "";
                await foreach (var result in searchResults.Value.GetResultsAsync())
                {
                    // Handle potential nulls in document fields
                    var fileName = result.Document.ContainsKey("fileName") ? result.Document["fileName"] : "Unknown";
                    var content = result.Document.ContainsKey("content") ? result.Document["content"] : "";
                    context += $"Document: {fileName}\nContent: {content}\n\n";
                }

                // 3. Generate response with Gemini
                var prompt = $"You are a helpful assistant. Use the following context to answer the user's question. If the answer is not in the context, say you don't know.\n\nContext:\n{context}\n\nQuestion: {query}\n\nAnswer:";
                
                var responseText = await _geminiService.GenerateContentAsync(prompt);

                // 4. Return response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { answer = responseText, contextUsed = context });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ChatFunction: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error processing request: {ex.Message}");
                return errorResponse;
            }
        }

        public class ChatRequest
        {
            public string Query { get; set; }
            public List<string>? FileNames { get; set; }
        }
    }
}
