namespace File.Service.Api.Services;

public class AudioCancellationService
{
    private readonly Dictionary<Guid, CancellationTokenSource> _cancellationTokens = new();
    private readonly object _lock = new();

    public CancellationToken RegisterAudio(Guid audioId)
    {
        lock (_lock)
        {
            if (_cancellationTokens.ContainsKey(audioId))
            {
                _cancellationTokens[audioId].Cancel();
                _cancellationTokens[audioId].Dispose();
            }

            var cts = new CancellationTokenSource();
            _cancellationTokens[audioId] = cts;
            return cts.Token;
        }
    }

    public void CancelAudio(Guid audioId)
    {
        lock (_lock)
        {
            if (_cancellationTokens.TryGetValue(audioId, out var cts))
            {
                cts.Cancel();
            }
        }
    }

    public bool IsCancelled(Guid audioId)
    {
        lock (_lock)
        {
            if (_cancellationTokens.TryGetValue(audioId, out var cts))
            {
                return cts.Token.IsCancellationRequested;
            }
            return false;
        }
    }

    public void UnregisterAudio(Guid audioId)
    {
        lock (_lock)
        {
            if (_cancellationTokens.TryGetValue(audioId, out var cts))
            {
                cts.Dispose();
                _cancellationTokens.Remove(audioId);
            }
        }
    }
}

