using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace DocumentClassification
{
    public class UploadFunction
    {
        private readonly ILogger _logger;

        public UploadFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadFunction>();
        }

        [Function("UploadFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequestData req)
        {
            _logger.LogInformation("UploadFunction processed a request.");

            try
            {
                // Get filename from query string or header
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                string filename = query["filename"];
                
                if (string.IsNullOrEmpty(filename))
                {
                     // Try header
                     if (req.Headers.TryGetValues("X-File-Name", out var headerValues))
                     {
                         foreach(var val in headerValues) { filename = val; break; }
                     }
                }

                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"upload-{Guid.NewGuid()}.pdf"; // Default fallback
                }

                string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
                string containerName = "documents";
                
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = containerClient.GetBlobClient(filename);

                await blobClient.UploadAsync(req.Body, true);

                _logger.LogInformation($"File {filename} uploaded to {containerName}/{filename}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { message = "File uploaded successfully", filename = filename });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error uploading file: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
