using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.UnitTests.Application;

public class TaskProcessorTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<ITaskCache> _cache = new();
    private readonly TaskProcessor _processor;

    public TaskProcessorTests()
    {
        _processor = new TaskProcessor(
            _tasks.Object,
            _cache.Object,
            new FixedTimeProvider(Now),
            NullLogger<TaskProcessor>.Instance);
    }

    [Fact]
    public async Task Process_marks_the_task_processed_and_invalidates_the_cache()
    {
        var taskId = Guid.NewGuid();
        _tasks.Setup(r => r.MarkProcessedAsync(taskId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var processed = await _processor.ProcessAsync(taskId);

        Assert.True(processed);
        _tasks.Verify(r => r.MarkProcessedAsync(taskId, Now, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_never_loads_the_task_so_it_cannot_overwrite_or_conflict_with_the_status()
    {
        var taskId = Guid.NewGuid();
        _tasks.Setup(r => r.MarkProcessedAsync(taskId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _processor.ProcessAsync(taskId);

        // Loading and saving the tracked task is what used to clash with a user's status change.
        _tasks.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_skips_a_task_that_was_already_processed_or_no_longer_exists()
    {
        var taskId = Guid.NewGuid();
        _tasks.Setup(r => r.MarkProcessedAsync(taskId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var processed = await _processor.ProcessAsync(taskId);

        Assert.False(processed);
        // Nothing changed, so the cached copy is still correct.
        _cache.Verify(c => c.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
