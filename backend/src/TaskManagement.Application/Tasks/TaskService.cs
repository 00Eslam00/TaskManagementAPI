using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks;

public class TaskService : ITaskService
{
    private const string DuplicateTitleMessage = "You already created a task with this title today.";

    private const string TaskNotFoundMessage = "Task was not found.";

    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskCache _cache;
    private readonly ITaskProcessingQueue _queue;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository tasks,
        IUnitOfWork unitOfWork,
        ITaskCache cache,
        ITaskProcessingQueue queue,
        TimeProvider timeProvider,
        ILogger<TaskService> logger)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _queue = queue;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var task = TaskItem.Create(userId, request.Title, request.Description, request.Priority, now);

        // Business rule: a user cannot create two tasks with the same title on the same (UTC) day.
        if (await _tasks.ExistsWithTitleOnDayAsync(userId, task.Title, task.CreatedOn, cancellationToken))
            throw new ConflictException(DuplicateTitleMessage);

        _tasks.Add(task);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException ex)
        {
            // Two identical requests arrived at the same time and both passed the check above.
            // The unique index in the database rejected the second one.
            throw new ConflictException(DuplicateTitleMessage, ex);
        }

        // Only queued once the task is safely in the database. The request does not wait
        // for the processing itself; the background worker picks the task up from here.
        await _queue.EnqueueAsync(task.Id, cancellationToken);

        _logger.LogInformation("Task {TaskId} created by user {UserId} and queued for processing", task.Id, userId);

        return task.ToResponse();
    }

    public async Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        // Cache-aside: try Redis first, and only go to the database on a miss.
        var cached = await _cache.GetAsync(taskId, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Task {TaskId} served from cache", taskId);

            // The cache is shared by all users, so ownership must be checked on a hit too.
            return cached.UserId == userId
                ? cached
                : throw new NotFoundException(TaskNotFoundMessage);
        }

        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
        var response = task.ToResponse();

        await _cache.SetAsync(response, cancellationToken);
        _logger.LogInformation("Task {TaskId} loaded from database and cached", taskId);

        return response;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tasks = await _tasks.GetByUserAsync(userId, cancellationToken);
        return tasks.Select(t => t.ToResponse()).ToList();
    }

    public async Task<TaskResponse> UpdateStatusAsync(Guid userId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

        if (task.ChangeStatus(request.Status))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate after the change is committed, so the next read reloads fresh data.
            await _cache.RemoveAsync(task.Id, cancellationToken);

            _logger.LogInformation("Task {TaskId} status changed to {Status}, cache invalidated", task.Id, task.Status);
        }

        return task.ToResponse();
    }


    private async Task<TaskItem> GetOwnedTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(taskId, cancellationToken);

        if (task is null || !task.IsOwnedBy(userId))
            throw new NotFoundException(TaskNotFoundMessage);

        return task;
    }
}
