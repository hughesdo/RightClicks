using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services
{
    /// <summary>
    /// Service for uploading files to file.io temporary file hosting.
    /// file.io provides anonymous, secure file sharing with automatic deletion after download.
    /// Files are deleted after first download or after expiration (default: 14 days).
    /// </summary>
    public class FileIoStorageService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string UploadUrl = "https://file.io";
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 2000; // 2 seconds

        /// <summary>
        /// Response from file.io upload API.
        /// Example: { "success": true, "key": "2ojE41", "link": "https://file.io/2ojE41", "expiry": "14 days" }
        /// </summary>
        private class UploadResponse
        {
            public bool success { get; set; }
            public string? key { get; set; }
            public string? link { get; set; }
            public string? expiry { get; set; }
            public int error { get; set; }
            public string? message { get; set; }
        }

        public FileIoStorageService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Allow time for large file uploads
            };

            Log.Information("FileIoStorageService initialized (file.io)");
        }

        /// <summary>
        /// Uploads a file to file.io and returns the public URL.
        /// Files are automatically deleted after first download or after 1 day (for fal.ai compatibility).
        /// </summary>
        public async Task<string> UploadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

            Log.Information("Uploading file to file.io: {FilePath} ({FileSize:F2} MB)", filePath, fileSizeMB);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Log.Debug("Sending upload request to file.io (attempt {Attempt}/{MaxRetries})...", attempt, MaxRetries);

                    using var formContent = new MultipartFormDataContent();

                    // Read file and add to form
                    var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    formContent.Add(fileContent, "file", Path.GetFileName(filePath));

                    // Set expiration to 1 day (for fal.ai processing time)
                    // file.io format: ?expires=1d (1 day), 1w (1 week), 1M (1 month), 1y (1 year)
                    var uploadUrlWithExpiry = $"{UploadUrl}/?expires=1d";

                    var response = await _httpClient.PostAsync(uploadUrlWithExpiry, formContent, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Debug("Upload response: {Response}", responseBody);

                        var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(responseBody);

                        if (uploadResponse?.success == true && !string.IsNullOrEmpty(uploadResponse.link))
                        {
                            Log.Information("File uploaded successfully to file.io: {Url} (expires: {Expiry})", 
                                uploadResponse.link, uploadResponse.expiry);
                            return uploadResponse.link;
                        }
                        else
                        {
                            var errorMsg = uploadResponse?.message ?? "Unknown error";
                            throw new HttpRequestException($"Upload failed: {errorMsg}");
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
                            throw new HttpRequestException($"Failed to upload file to file.io: {response.StatusCode} - {errorBody}");
                        }
                    }
                }
                catch (Exception ex) when (attempt < MaxRetries && ex is not OperationCanceledException)
                {
                    Log.Warning(ex, "Upload attempt {Attempt}/{MaxRetries} failed, retrying...", attempt, MaxRetries);
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
            }

            throw new HttpRequestException("Failed to upload file to file.io after all retry attempts");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

