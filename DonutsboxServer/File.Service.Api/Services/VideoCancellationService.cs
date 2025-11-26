using System.Collections.Concurrent;

namespace File.Service.Api.Services;

/// <summary>
/// Service to track cancelled video processing requests
/// </summary>
public class VideoCancellationService
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens = new();
    private readonly ConcurrentDictionary<Guid, bool> _cancelledVideos = new();

    /// <summary>
    /// Register a video for processing and get a cancellation token
    /// </summary>
    public CancellationToken RegisterVideo(Guid videoId)
    {
        var cts = new CancellationTokenSource();
        _cancellationTokens[videoId] = cts;
        return cts.Token;
    }

    /// <summary>
    /// Cancel processing for a video
    /// </summary>
    public void CancelVideo(Guid videoId, string reason)
    {
        _cancelledVideos[videoId] = true;
        
        if (_cancellationTokens.TryGetValue(videoId, out var cts))
        {
            cts.Cancel();
        }
    }

    /// <summary>
    /// Check if a video processing was cancelled
    /// </summary>
    public bool IsCancelled(Guid videoId)
    {
        return _cancelledVideos.TryGetValue(videoId, out var cancelled) && cancelled;
    }

    /// <summary>
    /// Unregister a video after processing is complete
    /// </summary>
    public void UnregisterVideo(Guid videoId)
    {
        _cancellationTokens.TryRemove(videoId, out var cts);
        cts?.Dispose();
        _cancelledVideos.TryRemove(videoId, out _);
    }
}

