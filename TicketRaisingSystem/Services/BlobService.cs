using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TicketRaisingSystem.Services
{
    public class BlobService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobService(string connectionString, string containerName)
        {
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob); // ← fixed: was None
        }

        // ── Upload image stream to blob, return public URL ────────────────────
        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            var blobName = $"{Guid.NewGuid()}-{fileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(imageStream, new BlobHttpHeaders
            {
                ContentType = GetContentType(fileName)
            });

            return blobClient.Uri.ToString();
        }

        // ── Download blob by URL and return as Base64 string ─────────────────
        public async Task<string?> DownloadImageAsBase64Async(string blobUrl)
        {
            try
            {
                // Extract blob name from full URL
                var uri = new Uri(blobUrl);
                var blobName = uri.Segments.Last();
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                    return null;

                // Download bytes
                using var ms = new MemoryStream();
                await blobClient.DownloadToAsync(ms);
                var bytes = ms.ToArray();

                // Get content type from blob properties
                var properties = await blobClient.GetPropertiesAsync();
                var contentType = properties.Value.ContentType ?? "image/jpeg";

                // Return as data URI
                return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Blob download error: {ex.Message}");
                return null;
            }
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}