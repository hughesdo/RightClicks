using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Service for uploading files to 0x0.st temporary file hosting.
/// Files are uploaded with 1-hour retention and hard-to-guess URLs.
/// Supports deletion via management tokens.
/// </summary>
public class FileHostingService : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string UploadUrl = "https://0x0.st";
    private const long MaxFileSizeBytes = 512L * 1024 * 1024; // 512 MB (0x0.st limit)
    private const int RetentionHours = 1; // 1 hour retention
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 2000; // 2 seconds

    /// <summary>
    /// Initializes a new instance of the FileHostingService.
    /// </summary>
    public FileHostingService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // 10 minute timeout for large uploads
        };
        
        // Use custom User-Agent as requested by 0x0.st
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "RightClicks/1.0 (https://github.com/hughesdo/RightClicks)");
        
        Log.Information("FileHostingService initialized (0x0.st)");
    }

    /// <summary>
    /// Uploads a file to 0x0.st and returns the URL and deletion token.
    /// </summary>
    /// <param name="filePath">Path to the file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (URL, DeleteToken).</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">If the file is too large.</exception>
    /// <exception cref="HttpRequestException">If the upload fails.</exception>
    public async Task<(string Url, string DeleteToken)> UploadFileAsync(
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            Log.Error("File not found for upload: {FilePath}", filePath);
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            var sizeMB = fileInfo.Length / (1024.0 * 1024.0);
            var maxMB = MaxFileSizeBytes / (1024.0 * 1024.0);
            Log.Error("File too large for upload: {FilePath} ({SizeMB:F2} MB, max: {MaxMB} MB)", 
                filePath, sizeMB, maxMB);
            throw new InvalidOperationException(
                $"File is too large for upload: {sizeMB:F2} MB (max: {maxMB} MB)");
        }

        Log.Information("Uploading file to 0x0.st: {FilePath} ({SizeMB:F2} MB)", 
            filePath, fileInfo.Length / (1024.0 * 1024.0));

        return await ExecuteWithRetryAsync(async () =>
        {
            using var form = new MultipartFormDataContent();
            
            // Add file
            var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            form.Add(fileContent, "file", Path.GetFileName(filePath));
            
            // Set 1-hour retention
            form.Add(new StringContent(RetentionHours.ToString()), "expires");

            // NOTE: Not using "secret" field - it triggers a server-side bug in 0x0.st
            // that causes crashes when retrieving files (segfault in fhost.c:139)
            
            // Upload
            var response = await _httpClient.PostAsync(UploadUrl, form, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                Log.Error("0x0.st upload failed: {StatusCode} - {ErrorBody}", 
                    response.StatusCode, errorBody);
                throw new HttpRequestException(
                    $"0x0.st upload failed: {response.StatusCode} - {errorBody}");
            }
            
            // Get URL from response body
            var url = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            
            // Get delete token from X-Token header
            string? deleteToken = null;
            if (response.Headers.TryGetValues("X-Token", out var tokens))
            {
                deleteToken = tokens.FirstOrDefault();
            }
            
            if (string.IsNullOrEmpty(deleteToken))
            {
                Log.Warning("No X-Token header received from 0x0.st (file cannot be deleted manually)");
                deleteToken = string.Empty;
            }
            
            Log.Information("File uploaded successfully: {Url} (token: {TokenPrefix}...)", 
                url, deleteToken.Length > 8 ? deleteToken.Substring(0, 8) : deleteToken);
            
            return (url, deleteToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Deletes a file from 0x0.st using the management token.
    /// </summary>
    /// <param name="fileUrl">URL of the file to delete.</param>
    /// <param name="deleteToken">Management token from upload response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteFileAsync(
        string fileUrl, 
        string deleteToken, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(deleteToken))
        {
            Log.Warning("Cannot delete file (no token): {FileUrl}", fileUrl);
            return;
        }

        Log.Information("Deleting file from 0x0.st: {FileUrl}", fileUrl);

        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(deleteToken), "token");
            form.Add(new StringContent(""), "delete");
            
            var response = await _httpClient.PostAsync(fileUrl, form, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                Log.Information("File deleted successfully: {FileUrl}", fileUrl);
            }
            else
            {
                Log.Warning("File deletion failed: {FileUrl} - {StatusCode}", 
                    fileUrl, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete file: {FileUrl} (will expire in {Hours} hour)",
                fileUrl, RetentionHours);
        }
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < MaxRetries &&
                (ex is HttpRequestException || ex is TaskCanceledException))
            {
                Log.Warning(ex, "HTTP request failed (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {DelayMs}ms...",
                    attempt, MaxRetries, ex.Message, RetryDelayMs);

                await Task.Delay(RetryDelayMs, cancellationToken);
            }
        }

        // This should never be reached, but satisfies the compiler
        throw new InvalidOperationException("Retry logic failed unexpectedly");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
