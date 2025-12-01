using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services
{
    /// <summary>
    /// Result from Cloudinary upload operation.
    /// </summary>
    public class CloudinaryUploadResult
    {
        public string SecureUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service for uploading files to Cloudinary cloud storage.
    /// Cloudinary is an enterprise-grade media management platform with reliable file hosting.
    /// Uploaded files are stored permanently (until manually deleted) and are globally distributed via CDN.
    /// </summary>
    public class CloudinaryStorageService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _cloudName;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 2000; // 2 seconds

        /// <summary>
        /// Response from Cloudinary upload API.
        /// Example: { "secure_url": "https://res.cloudinary.com/...", "public_id": "...", "resource_type": "video" }
        /// </summary>
        private class UploadResponse
        {
            public string? secure_url { get; set; }
            public string? public_id { get; set; }
            public string? resource_type { get; set; }
            public string? format { get; set; }
            public long bytes { get; set; }
            public string? error { get; set; }
        }

        /// <summary>
        /// Response from Cloudinary destroy API.
        /// Example: { "result": "ok" } or { "result": "not found" }
        /// </summary>
        private class DestroyResponse
        {
            public string? result { get; set; }
            public string? error { get; set; }
        }

        public CloudinaryStorageService(string cloudName, string apiKey, string apiSecret)
        {
            if (string.IsNullOrWhiteSpace(cloudName))
                throw new ArgumentException("Cloud name is required", nameof(cloudName));
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key is required", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(apiSecret))
                throw new ArgumentException("API secret is required", nameof(apiSecret));

            _cloudName = cloudName;
            _apiKey = apiKey;
            _apiSecret = apiSecret;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10) // Allow time for large file uploads
            };

            Log.Information("CloudinaryStorageService initialized (cloud: {CloudName})", _cloudName);
        }

        /// <summary>
        /// Uploads a file to Cloudinary and returns the upload result with secure URL and public ID.
        /// Files are stored permanently until manually deleted.
        /// </summary>
        public async Task<CloudinaryUploadResult> UploadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

            Log.Information("Uploading file to Cloudinary: {FilePath} ({FileSize:F2} MB)", filePath, fileSizeMB);

            // Determine resource type based on file extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var resourceType = extension switch
            {
                ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => "video",
                ".mp3" or ".wav" or ".m4a" or ".flac" or ".aac" => "video", // Cloudinary uses 'video' for audio too
                _ => "auto" // Let Cloudinary auto-detect
            };

            var uploadUrl = $"https://api.cloudinary.com/v1_1/{_cloudName}/{resourceType}/upload";

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Log.Debug("Sending upload request to Cloudinary (attempt {Attempt}/{MaxRetries})...", attempt, MaxRetries);

                    using var formContent = new MultipartFormDataContent();

                    // Use unsigned upload with preset "RightClicks"
                    var presetName = "RightClicks";
                    Log.Information("Using Cloudinary unsigned upload with preset: {PresetName}", presetName);
                    Log.Debug("Upload URL: {UploadUrl}", uploadUrl);

                    // Add upload_preset parameter with explicit content disposition
                    var presetContent = new StringContent(presetName);
                    presetContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"upload_preset\""
                    };
                    formContent.Add(presetContent);

                    // Read file and add to form
                    var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    formContent.Add(fileContent, "file", Path.GetFileName(filePath));

                    Log.Debug("Form data: upload_preset={PresetName}, file={FileName} ({FileSize} bytes)",
                        presetName, Path.GetFileName(filePath), fileBytes.Length);

                    // Log the actual form content for debugging
                    Log.Debug("MultipartFormDataContent boundary: {Boundary}", formContent.Headers.ContentType?.Parameters.FirstOrDefault(p => p.Name == "boundary")?.Value);

                    var response = await _httpClient.PostAsync(uploadUrl, formContent, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Debug("Upload response: {Response}", responseBody);

                        var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(responseBody);

                        if (!string.IsNullOrEmpty(uploadResponse?.secure_url))
                        {
                            Log.Information("File uploaded successfully to Cloudinary: {Url} (public_id: {PublicId}, resource_type: {ResourceType})",
                                uploadResponse.secure_url, uploadResponse.public_id, uploadResponse.resource_type);

                            return new CloudinaryUploadResult
                            {
                                SecureUrl = uploadResponse.secure_url,
                                PublicId = uploadResponse.public_id ?? string.Empty,
                                ResourceType = uploadResponse.resource_type ?? "image"
                            };
                        }
                        else if (!string.IsNullOrEmpty(uploadResponse?.error))
                        {
                            throw new HttpRequestException($"Upload failed: {uploadResponse.error}");
                        }
                        else
                        {
                            throw new HttpRequestException("Upload failed: No URL returned");
                        }
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Warning("Upload failed (attempt {Attempt}/{MaxRetries}): {StatusCode} - {Error}", 
                            attempt, MaxRetries, response.StatusCode, errorBody);

                        if (attempt < MaxRetries)
                        {
                            await Task.Delay(RetryDelayMs, cancellationToken);
                        }
                        else
                        {
                            throw new HttpRequestException($"Failed to upload file to Cloudinary: {response.StatusCode} - {errorBody}");
                        }
                    }
                }
                catch (Exception ex) when (attempt < MaxRetries && ex is not OperationCanceledException)
                {
                    Log.Warning(ex, "Upload attempt {Attempt}/{MaxRetries} failed, retrying...", attempt, MaxRetries);
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
            }

            throw new HttpRequestException("Failed to upload file to Cloudinary after all retry attempts");
        }

        /// <summary>
        /// Deletes a file from Cloudinary using its public ID.
        /// This permanently removes the file from Cloudinary storage.
        /// </summary>
        /// <param name="publicId">The public_id returned from the upload operation</param>
        /// <param name="resourceType">The resource type (image, video, raw)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteFileAsync(string publicId, string resourceType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                Log.Warning("Cannot delete file: public_id is null or empty");
                return false;
            }

            Log.Information("Deleting file from Cloudinary: {PublicId} (resource_type: {ResourceType})", publicId, resourceType);

            var destroyUrl = $"https://api.cloudinary.com/v1_1/{_cloudName}/{resourceType}/destroy";

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Log.Debug("Sending destroy request to Cloudinary (attempt {Attempt}/{MaxRetries})...", attempt, MaxRetries);

                    // Generate timestamp (UNIX time in seconds)
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                    // Generate signature: SHA256(public_id={publicId}&timestamp={timestamp}{apiSecret})
                    var signaturePayload = $"public_id={publicId}&timestamp={timestamp}{_apiSecret}";
                    var signature = GenerateSha256Hash(signaturePayload);

                    Log.Debug("Destroy URL: {DestroyUrl}", destroyUrl);
                    Log.Debug("Signature payload (without secret): public_id={PublicId}&timestamp={Timestamp}", publicId, timestamp);

                    // Create form data with required parameters
                    using var formContent = new MultipartFormDataContent();

                    // Add public_id parameter
                    var publicIdContent = new StringContent(publicId);
                    publicIdContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"public_id\""
                    };
                    formContent.Add(publicIdContent);

                    // Add api_key parameter
                    var apiKeyContent = new StringContent(_apiKey);
                    apiKeyContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"api_key\""
                    };
                    formContent.Add(apiKeyContent);

                    // Add timestamp parameter
                    var timestampContent = new StringContent(timestamp);
                    timestampContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"timestamp\""
                    };
                    formContent.Add(timestampContent);

                    // Add signature parameter
                    var signatureContent = new StringContent(signature);
                    signatureContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"signature\""
                    };
                    formContent.Add(signatureContent);

                    var response = await _httpClient.PostAsync(destroyUrl, formContent, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Debug("Destroy response: {Response}", responseBody);

                        var destroyResponse = JsonSerializer.Deserialize<DestroyResponse>(responseBody);

                        if (destroyResponse?.result == "ok")
                        {
                            Log.Information("File deleted successfully from Cloudinary: {PublicId}", publicId);
                            return true;
                        }
                        else if (destroyResponse?.result == "not found")
                        {
                            Log.Warning("File not found on Cloudinary (may have been already deleted): {PublicId}", publicId);
                            return true; // Consider this a success - file doesn't exist anymore
                        }
                        else if (!string.IsNullOrEmpty(destroyResponse?.error))
                        {
                            Log.Error("Cloudinary deletion failed: {Error}", destroyResponse.error);
                            return false;
                        }
                        else
                        {
                            Log.Warning("Unexpected destroy response: {Response}", responseBody);
                            return false;
                        }
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Warning("Destroy failed (attempt {Attempt}/{MaxRetries}): {StatusCode} - {Error}",
                            attempt, MaxRetries, response.StatusCode, errorBody);

                        if (attempt < MaxRetries)
                        {
                            await Task.Delay(RetryDelayMs, cancellationToken);
                        }
                        else
                        {
                            Log.Error("Failed to delete file from Cloudinary after all retries: {StatusCode} - {Error}",
                                response.StatusCode, errorBody);
                            return false;
                        }
                    }
                }
                catch (Exception ex) when (attempt < MaxRetries && ex is not OperationCanceledException)
                {
                    Log.Warning(ex, "Destroy attempt {Attempt}/{MaxRetries} failed, retrying...", attempt, MaxRetries);
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete file from Cloudinary: {PublicId}", publicId);
                    return false;
                }
            }

            return false;
        }

        private string GenerateSha1Hash(string input)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha1.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string GenerateSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

