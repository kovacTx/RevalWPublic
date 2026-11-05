using RevalW.Shared.Model;
using System.Collections.Concurrent;

namespace RevalW.Services.DocumentsAPI.Services;

public class GenerationProgressService : IDisposable
{
    private class ProgressQueue
    {
        public ConcurrentQueue<string> Queue { get; } = new();
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;
        public DocumentCreationResult? Result { get; set; } = null;
        public DateTime? CompletedAtUtc { get; set; } = null;
    }

    private readonly ConcurrentDictionary<string, ProgressQueue> _map = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _ttl;

    // ttl: how long to keep completed entries before removal (default 5 minutes)
    public GenerationProgressService(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
        // run cleanup every minute
        _cleanupTimer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public void Create(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return;
        _map[operationId] = new ProgressQueue();
    }

    public void TryAppend(string? operationId, string message)
    {
        if (string.IsNullOrEmpty(operationId)) return;
        if (!_map.TryGetValue(operationId, out var pq)) return;
        pq.Queue.Enqueue($"{DateTime.UtcNow:O} {message}");
        pq.LastUpdateUtc = DateTime.UtcNow;
    }

    // Returns and clears current buffered messages for operationId.
    public IEnumerable<string> GetAndClear(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return [];
        if (!_map.TryGetValue(operationId, out var pq)) return [];

        var list = new List<string>();
        while (pq.Queue.TryDequeue(out var msg))
            list.Add(msg);
        pq.LastUpdateUtc = DateTime.UtcNow;
        return list;
    }

    public void TrySetResult(string? operationId, DocumentCreationResult result)
    {
        if (string.IsNullOrEmpty(operationId)) return;
        if (_map.TryGetValue(operationId, out var pq))
        {
            pq.Result = result;
            pq.CompletedAtUtc = DateTime.UtcNow;
        }
    }

    public DocumentCreationResult? GetResult(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return null;
        if (!_map.TryGetValue(operationId, out var pq)) return null;
        return pq.Result;
    }

    public void Remove(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return;
        _map.TryRemove(operationId, out _);
    }

    private void Cleanup()
    {
        try
        {
            var now = DateTime.UtcNow;
            var keys = _map.ToArray();
            foreach (var kv in keys)
            {
                var id = kv.Key;
                var pq = kv.Value;
                // remove completed entries older than TTL
                if (pq.Result is not null && pq.CompletedAtUtc.HasValue && now - pq.CompletedAtUtc.Value > _ttl)
                {
                    _map.TryRemove(id, out _);
                    continue;
                }
                // remove stale entries that have no activity for longer than 4x TTL (safety)
                if (pq.Result is null && now - pq.LastUpdateUtc > _ttl * 4)
                {
                    _map.TryRemove(id, out _);
                }
            }
        }
        catch
        {
            // swallow - cleanup best-effort
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}