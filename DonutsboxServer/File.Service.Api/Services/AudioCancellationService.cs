using System.Collections.Concurrent;

namespace File.Service.Api.Services;

public class AudioCancellationService
{
    // Используем ConcurrentDictionary для лучшей производительности при параллельных операциях
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens = new();
    private readonly ConcurrentDictionary<Guid, bool> _cancelledAudios = new();

    public CancellationToken RegisterAudio(Guid audioId)
    {
        // Если уже есть токен, отменяем и освобождаем его
        if (_cancellationTokens.TryRemove(audioId, out var oldCts))
        {
            try
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            catch { /* ignore */ }
        }

        var cts = new CancellationTokenSource();
        _cancellationTokens[audioId] = cts;
        return cts.Token;
    }

    public void CancelAudio(Guid audioId)
    {
        _cancelledAudios[audioId] = true;
        
        if (_cancellationTokens.TryGetValue(audioId, out var cts))
        {
            cts.Cancel();
        }
    }

    public bool IsCancelled(Guid audioId)
    {
        return _cancelledAudios.TryGetValue(audioId, out var cancelled) && cancelled;
    }

    public void UnregisterAudio(Guid audioId)
    {
        if (_cancellationTokens.TryRemove(audioId, out var cts))
        {
            try
            {
                cts.Dispose();
            }
            catch { /* ignore */ }
        }
        _cancelledAudios.TryRemove(audioId, out _);
    }
}

