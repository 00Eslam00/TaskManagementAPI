using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Tasks;

namespace TaskManagement.Infrastructure.Caching;


// Stores each task as JSON under the key "task:{id}".

public class RedisTaskCache : ITaskCache
{
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RedisTaskCache> _logger;

    // Simple circuit breaker: after a failure, reads and writes skip Redis until this time,
    // so requests do not each wait for a timeout while Redis is down.
    private long _skipUntilTicks;

    // Tasks whose invalidation failed. Redis may still hold an outdated copy of them when it
    // comes back, so they are read from the database until a fresh copy has been written.
    private readonly ConcurrentDictionary<Guid, byte> _failedRemovals = new();

    public RedisTaskCache(
        IDistributedCache cache,
        IOptions<CacheOptions> options,
        TimeProvider timeProvider,
        ILogger<RedisTaskCache> logger)
    {
        _cache = cache;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TaskResponse?> GetAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (IsSkippingRedis() || _failedRemovals.ContainsKey(taskId))
            return null;

        try
        {
            var json = await _cache.GetStringAsync(Key(taskId), cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<TaskResponse>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnFailure(ex, "read", taskId);
            return null;
        }
    }

    public async Task SetAsync(TaskResponse task, CancellationToken cancellationToken = default)
    {
        if (IsSkippingRedis())
            return;

        try
        {
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.TaskExpirationMinutes)
            };

            await _cache.SetStringAsync(Key(task.Id), JsonSerializer.Serialize(task), entryOptions, cancellationToken);

            // The fresh copy has replaced any outdated one.
            _failedRemovals.TryRemove(task.Id, out _);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnFailure(ex, "write", task.Id);
        }
    }

    public async Task RemoveAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        // Always attempted, even while the breaker is open, because a skipped invalidation
        // would leave an outdated entry behind.
        try
        {
            await _cache.RemoveAsync(Key(taskId), cancellationToken);
            _failedRemovals.TryRemove(taskId, out _);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _failedRemovals[taskId] = 0;
            OnFailure(ex, "remove", taskId);
        }
    }

    private bool IsSkippingRedis() =>
        _timeProvider.GetUtcNow().UtcTicks < Interlocked.Read(ref _skipUntilTicks);

    private void OnFailure(Exception exception, string operation, Guid taskId)
    {
        var retryAfter = TimeSpan.FromSeconds(_options.RetryAfterFailureSeconds);
        Interlocked.Exchange(ref _skipUntilTicks, _timeProvider.GetUtcNow().Add(retryAfter).UtcTicks);

        _logger.LogWarning(exception,
            "Redis {Operation} failed for task {TaskId}. Using the database only for the next {Seconds} seconds",
            operation, taskId, retryAfter.TotalSeconds);
    }

    private static string Key(Guid taskId) => $"task:{taskId}";
}
