using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.UnitTests.Application;

public class TaskServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITaskCache> _cache = new();
    private readonly Mock<ITaskProcessingQueue> _queue = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(
            _tasks.Object,
            _unitOfWork.Object,
            _cache.Object,
            _queue.Object,
            new FixedTimeProvider(Now),
            NullLogger<TaskService>.Instance);
    }

    private TaskItem ExistingTask(Guid? ownerId = null)
    {
        var task = TaskItem.Create(ownerId ?? UserId, "Write report", null, TaskPriority.High, Now);
        _tasks.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        return task;
    }

    [Fact]
    public async Task Create_saves_the_task_and_queues_it_for_processing()
    {
        var response = await _service.CreateAsync(UserId, new CreateTaskRequest { Title = " Write report ", Priority = TaskPriority.High });

        Assert.Equal("Write report", response.Title);
        Assert.Equal(TaskItemStatus.Pending, response.Status);
        Assert.Equal(UserId, response.UserId);
        Assert.Equal(Now, response.CreatedAt);

        _tasks.Verify(r => r.Add(It.Is<TaskItem>(t => t.Id == response.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _queue.Verify(q => q.EnqueueAsync(response.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_title_on_the_same_day()
    {
        _tasks.Setup(r => r.ExistsWithTitleOnDayAsync(UserId, "Write report", DateOnly.FromDateTime(Now), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(UserId, new CreateTaskRequest { Title = "Write report" }));

        _tasks.Verify(r => r.Add(It.IsAny<TaskItem>()), Times.Never);
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_reports_a_duplicate_when_the_database_rejects_a_concurrent_insert()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("unique index violated"));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(UserId, new CreateTaskRequest { Title = "Write report" }));

        Assert.Equal("You already created a task with this title today.", exception.Message);
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_loads_from_the_database_and_caches_on_a_cache_miss()
    {
        var task = ExistingTask();

        var response = await _service.GetByIdAsync(UserId, task.Id);

        Assert.Equal(task.Id, response.Id);
        _cache.Verify(c => c.SetAsync(It.Is<TaskResponse>(t => t.Id == task.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_returns_the_cached_task_without_touching_the_database()
    {
        var cached = TaskItem.Create(UserId, "Cached", null, TaskPriority.Low, Now).ToResponse();
        _cache.Setup(c => c.GetAsync(cached.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var response = await _service.GetByIdAsync(UserId, cached.Id);

        Assert.Same(cached, response);
        _tasks.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_hides_a_cached_task_that_belongs_to_another_user()
    {
        var cached = TaskItem.Create(OtherUserId, "Not yours", null, TaskPriority.Low, Now).ToResponse();
        _cache.Setup(c => c.GetAsync(cached.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(UserId, cached.Id));
    }

    [Fact]
    public async Task GetById_hides_another_users_task_and_does_not_cache_it()
    {
        var task = ExistingTask(OtherUserId);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(UserId, task.Id));

        _cache.Verify(c => c.SetAsync(It.IsAny<TaskResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_reports_a_missing_task_as_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(UserId, Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateStatus_saves_the_change_and_invalidates_the_cache()
    {
        var task = ExistingTask();

        var response = await _service.UpdateStatusAsync(UserId, task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.InProgress });

        Assert.Equal(TaskItemStatus.InProgress, response.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_to_the_current_status_does_nothing()
    {
        var task = ExistingTask();

        await _service.UpdateStatusAsync(UserId, task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.Pending });

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_rejects_another_users_task()
    {
        var task = ExistingTask(OtherUserId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateStatusAsync(UserId, task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.Done }));

        Assert.Equal(TaskItemStatus.Pending, task.Status);
    }

    [Fact]
    public async Task UpdateStatus_reports_a_conflict_when_another_request_changed_the_status_first()
    {
        var task = ExistingTask();
        // What AppDbContext throws when the Status concurrency token no longer matches.
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("The record was changed by another request."));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateStatusAsync(UserId, task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.InProgress }));

        Assert.Equal("This task was changed by another request at the same time. Reload it and try again.", exception.Message);
        // Nothing was saved, so the cached copy (if any) is still correct and is left alone.
        _cache.Verify(c => c.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_rejects_changing_a_done_task()
    {
        var task = ExistingTask();
        task.ChangeStatus(TaskItemStatus.Done);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateStatusAsync(UserId, task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.Pending }));
    }

    [Fact]
    public async Task GetAll_returns_the_users_tasks_in_repository_order()
    {
        var high = TaskItem.Create(UserId, "High", null, TaskPriority.High, Now);
        var low = TaskItem.Create(UserId, "Low", null, TaskPriority.Low, Now);
        _tasks.Setup(r => r.GetByUserAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([high, low]);

        var responses = await _service.GetAllAsync(UserId);

        Assert.Equal(["High", "Low"], responses.Select(t => t.Title));
    }
}
