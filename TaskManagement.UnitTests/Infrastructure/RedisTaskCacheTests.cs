using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Caching;

namespace TaskManagement.UnitTests.Infrastructure;

public class RedisTaskCacheTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IDistributedCache> _redis = new();
    private readonly FixedTimeProvider _clock = new(Now);
    private readonly RedisTaskCache _cache;
    private readonly TaskResponse _task = TaskItem.Create(Guid.NewGuid(), "Write report", null, TaskPriority.High, Now).ToResponse();

    public RedisTaskCacheTests()
    {
        var options = Options.Create(new CacheOptions { TaskExpirationMinutes = 10, RetryAfterFailureSeconds = 30 });
        _cache = new RedisTaskCache(_redis.Object, options, _clock, NullLogger<RedisTaskCache>.Instance);
    }

    private string Key => $"task:{_task.Id}";

    private void RedisHoldsTheTask() =>
        _redis.Setup(r => r.GetAsync(Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_task)));

    private void RedisIsDown()
    {
        var failure = new InvalidOperationException("Redis is unavailable");
        _redis.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        _redis.Setup(r => r.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        _redis.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
    }

    private void RedisIsBackUp()
    {
        _redis.Reset();
        RedisHoldsTheTask();
    }

    [Fact]
    public async Task Get_returns_the_cached_task()
    {
        RedisHoldsTheTask();

        Assert.Equal(_task, await _cache.GetAsync(_task.Id));
    }

    [Fact]
    public async Task Get_returns_null_on_a_cache_miss()
    {
        Assert.Null(await _cache.GetAsync(_task.Id));
    }

    [Fact]
    public async Task Set_stores_the_task_with_the_configured_expiry()
    {
        await _cache.SetAsync(_task);

        _redis.Verify(r => r.SetAsync(
            Key,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(10)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_redis_failure_is_swallowed_and_treated_as_a_cache_miss()
    {
        RedisIsDown();

        Assert.Null(await _cache.GetAsync(_task.Id));
        await _cache.SetAsync(_task);
        await _cache.RemoveAsync(_task.Id);
    }

    [Fact]
    public async Task After_a_failure_redis_is_skipped_until_the_retry_window_has_passed()
    {
        RedisIsDown();
        await _cache.GetAsync(_task.Id);
        RedisIsBackUp();

        Assert.Null(await _cache.GetAsync(_task.Id));
        _redis.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _clock.Advance(TimeSpan.FromSeconds(31));

        Assert.Equal(_task, await _cache.GetAsync(_task.Id));
    }

    [Fact]
    public async Task A_task_whose_invalidation_failed_is_not_read_from_redis_until_it_is_cached_again()
    {
        RedisIsDown();
        await _cache.RemoveAsync(_task.Id);

        // Redis is back and still holds the outdated copy.
        RedisIsBackUp();
        _clock.Advance(TimeSpan.FromSeconds(31));

        Assert.Null(await _cache.GetAsync(_task.Id));

        await _cache.SetAsync(_task);

        Assert.Equal(_task, await _cache.GetAsync(_task.Id));
    }
}
