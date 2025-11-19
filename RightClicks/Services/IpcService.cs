using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Inter-process communication service using named pipes.
/// Allows new instances to send job requests to the running instance.
/// </summary>
public class IpcService : IDisposable
{
    private const string PipeName = "RightClicks_IPC_Pipe";
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    /// <summary>
    /// Event fired when a job request is received from another instance.
    /// </summary>
    public event EventHandler<JobRequest>? JobRequestReceived;

    /// <summary>
    /// Start listening for job requests from other instances.
    /// </summary>
    public void StartListening()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenForConnectionsAsync(_cancellationTokenSource.Token));
        Log.Information("IPC service started listening on pipe: {PipeName}", PipeName);
    }

    /// <summary>
    /// Stop listening for job requests.
    /// </summary>
    public void StopListening()
    {
        _cancellationTokenSource?.Cancel();
        _pipeServer?.Dispose();
        _listenerTask?.Wait(TimeSpan.FromSeconds(2));
        Log.Information("IPC service stopped listening");
    }

    /// <summary>
    /// Listen for incoming connections from other instances.
    /// </summary>
    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create a new pipe server for each connection
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                Log.Debug("Waiting for IPC connection...");

                // Wait for a client to connect
                await _pipeServer.WaitForConnectionAsync(cancellationToken);

                Log.Debug("IPC client connected");

                // Read the job request (use ReadLineAsync instead of ReadToEndAsync)
                using var reader = new StreamReader(_pipeServer, Encoding.UTF8, leaveOpen: true);
                string? message = await reader.ReadLineAsync();

                Log.Debug("IPC message received: {Message}", message ?? "(null)");

                // Parse the job request
                if (!string.IsNullOrEmpty(message))
                {
                    var jobRequest = ParseJobRequest(message);
                    if (jobRequest != null)
                    {
                        Log.Information("IPC job request received: Feature={FeatureId}, File={FilePath}",
                            jobRequest.FeatureId, jobRequest.FilePath);

                        // Fire the event
                        JobRequestReceived?.Invoke(this, jobRequest);

                        // Send acknowledgment
                        using var writer = new StreamWriter(_pipeServer, Encoding.UTF8, leaveOpen: true);
                        await writer.WriteLineAsync("OK");
                        await writer.FlushAsync();
                    }
                }

                // Disconnect and dispose
                _pipeServer.Disconnect();
                _pipeServer.Dispose();
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in IPC listener");
                await Task.Delay(1000, cancellationToken); // Wait before retrying
            }
        }
    }

    /// <summary>
    /// Send a job request to the running instance.
    /// Returns true if successful, false if no instance is running.
    /// </summary>
    public static async Task<bool> SendJobRequestAsync(string featureId, string filePath, int timeoutMs = 5000)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
            
            Log.Debug("Attempting to connect to IPC pipe...");
            
            // Try to connect to the running instance
            await pipeClient.ConnectAsync(timeoutMs);
            
            Log.Debug("Connected to IPC pipe");

            // Send the job request
            string message = $"{featureId}|{filePath}";
            using var writer = new StreamWriter(pipeClient, Encoding.UTF8);
            await writer.WriteLineAsync(message);
            await writer.FlushAsync();

            Log.Debug("IPC message sent: {Message}", message);

            // Wait for acknowledgment
            using var reader = new StreamReader(pipeClient, Encoding.UTF8);
            string? response = await reader.ReadLineAsync();

            Log.Information("IPC job request sent successfully: Feature={FeatureId}, File={FilePath}", 
                featureId, filePath);

            return response == "OK";
        }
        catch (TimeoutException)
        {
            Log.Debug("IPC connection timeout - no running instance found");
            return false;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to send IPC job request - no running instance found");
            return false;
        }
    }

    /// <summary>
    /// Parse a job request message.
    /// Format: "FeatureId|FilePath"
    /// </summary>
    private JobRequest? ParseJobRequest(string message)
    {
        try
        {
            var parts = message.Split('|');
            if (parts.Length == 2)
            {
                return new JobRequest
                {
                    FeatureId = parts[0],
                    FilePath = parts[1]
                };
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse IPC job request: {Message}", message);
        }

        return null;
    }

    public void Dispose()
    {
        StopListening();
        _cancellationTokenSource?.Dispose();
        _pipeServer?.Dispose();
    }
}

/// <summary>
/// Represents a job request received via IPC.
/// </summary>
public class JobRequest
{
    public string FeatureId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

