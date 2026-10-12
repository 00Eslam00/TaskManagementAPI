using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.UnitTests.Application;

public class TaskProcessorTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITaskCache> _cache = new();
    private readonly TaskProcessor _processor;

    public TaskProcessorTests()
    {
        _processor = new TaskProcessor(
            _tasks.Object,
            _unitOfWork.Object,
            _cache.Object,
            new FixedTimeProvider(Now),
            NullLogger<TaskProcessor>.Instance);
    }

    private TaskItem ExistingTask()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write report", null, TaskPriority.Medium, Now.AddMinutes(-1));
        _tasks.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        return task;
    }

    [Fact]
    public async Task Process_marks_the_task_processed_saves_and_invalidates_the_cache()
    {
        var task = ExistingTask();

        var processed = await _processor.ProcessAsync(task.Id);

        Assert.True(processed);
        Assert.Equal(Now, task.ProcessedAt);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_skips_a_task_that_was_already_processed()
    {
        var task = ExistingTask();
        task.MarkProcessed(Now.AddSeconds(-30));

        var processed = await _processor.ProcessAsync(task.Id);

        Assert.False(processed);
        Assert.Equal(Now.AddSeconds(-30), task.ProcessedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_skips_a_task_that_no_longer_exists()
    {
        var processed = await _processor.ProcessAsync(Guid.NewGuid());

        Assert.False(processed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
