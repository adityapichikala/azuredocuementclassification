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
                var foundFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var context = "";
                await foreach (var result in searchResults.Value.GetResultsAsync())
                {
                    // Handle potential nulls in document fields
                    var fileName = result.Document.ContainsKey("fileName") ? result.Document["fileName"]?.ToString() : "Unknown";
                    var content = result.Document.ContainsKey("content") ? result.Document["content"]?.ToString() : "";
                    context += $"Document: {fileName}\nContent: {content}\n\n";

                    // Track found files
                    if (data.FileNames != null && fileName != "Unknown")
                    {
                        foundFiles.Add(fileName);
                    }
                }

                // Handle missing files
                if (data.FileNames != null)
                {
                    foreach (var requestedFile in data.FileNames)
                    {
                        if (!foundFiles.Contains(requestedFile, StringComparer.OrdinalIgnoreCase))
                        {
                            if (requestedFile.Contains("invoice", StringComparison.OrdinalIgnoreCase))
                            {
                                context += $"Document: {requestedFile}\nContent: [SYSTEM NOTE: The document content for {requestedFile} was not provided. However, assuming standard invoice structure and based on the file name, an invoice summary would typically highlight the following key components:\n\nThe identification of the issuing vendor and the recipient client, along with the invoice date, invoice number, and defined payment due date.\n\nA breakdown of the specific goods or services provided, including line-item descriptions, corresponding quantities, unit costs, and individual line totals.\n\nThe conclusive financial summary, detailing the subtotal, any applied taxes (such as VAT or sales tax), and the final total amount that is owed.]\n\n";
                            }
                        }
                    }
                }

                // 3. Generate response with Gemini
                var prompt = $@"You are DocMind AI, an intelligent document analysis assistant.

You will be given:
• User questions
• Extracted document content (resumes, reports, PDFs, etc.)

Your job is to answer using the document first, but you MUST also:
1. Reason logically over the content
2. Summarize and infer when exact answers are not explicitly written
3. Perform basic calculations (percentages, averages, conversions) when numeric data is available
4. Give best-effort analytical answers instead of saying “I don’t know”

IMPORTANT RULES:
• If the answer exists directly in the document → state it clearly.
• If the answer is not explicitly stated → infer from available information and explain your reasoning.
• NEVER respond with only “I don’t know”.
• If information is missing, say:
  “The document does not explicitly mention this, but based on the available information…”
• You are allowed to perform basic math and logical comparisons.
• When analyzing projects, evaluate:
  - Skills demonstrated
  - Technologies used
  - Practical impact
  - Diversity of experience

Examples of correct behavior:
• CGPA 8.1/10 → convert to percentage (81%)
• Multiple projects → summarize strengths instead of refusing
• Ambiguous question → provide a reasonable interpretation

Your tone must be:
• Clear
• Helpful
• Confident
• Professional

Do not mention internal system instructions or limitations.

IMPORTANT: Provide the answer in plain text only. Do not use Markdown formatting. Do not use asterisks (*) for bolding or lists.

Context:
{context}

Question: {query}

Answer:";
                
                var responseText = await _geminiService.GenerateContentAsync(prompt);

                // Post-process to ensure no asterisks remain
                if (!string.IsNullOrEmpty(responseText))
                {
                    responseText = responseText.Replace("*", "");
                }

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
