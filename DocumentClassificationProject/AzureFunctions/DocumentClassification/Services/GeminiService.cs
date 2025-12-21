using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentClassification.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["GeminiApiKey"];
            // Allow null for now to avoid crashing if key is not set immediately, but log warning
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("GeminiApiKey is missing from configuration.");
            }
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<float[]> GenerateEmbeddingsAsync(string text)
        {
            if (string.IsNullOrEmpty(_apiKey)) throw new InvalidOperationException("GeminiApiKey is not configured.");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_apiKey}";

            var requestBody = new
            {
                model = "models/text-embedding-004",
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Gemini API Error (Embeddings): {error}");
                throw new Exception($"Gemini API Error: {response.StatusCode} - {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            
            if (doc.RootElement.TryGetProperty("embedding", out var embeddingElement) && 
                embeddingElement.TryGetProperty("values", out var values))
            {
                var embeddings = new float[values.GetArrayLength()];
                int i = 0;
                foreach (var value in values.EnumerateArray())
                {
                    embeddings[i++] = value.GetSingle();
                }
                return embeddings;
            }
            
            throw new Exception("Invalid response format from Gemini API (Embeddings).");
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey)) throw new InvalidOperationException("GeminiApiKey is not configured.");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Gemini API Error (Chat): {error}");
                throw new Exception($"Gemini API Error: {response.StatusCode} - {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            
            try 
            {
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                    return text;
                }
                return "No response generated.";
            }
            catch (Exception ex)
            {
                 _logger.LogError($"Error parsing Gemini response: {ex.Message}");
                 return "Error parsing response.";
            }
        }
    }
}
