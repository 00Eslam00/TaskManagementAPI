using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks;

public class TaskProcessor : ITaskProcessor
{
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TaskProcessor> _logger;

    public TaskProcessor(
        ITaskRepository tasks,
        IUnitOfWork unitOfWork,
        ITaskCache cache,
        TimeProvider timeProvider,
        ILogger<TaskProcessor> logger)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} no longer exists, skipping", taskId);
            return false;
        }

        // False when the task was already processed.
        if (!task.MarkProcessed(_timeProvider.GetUtcNow().UtcDateTime))
        {
            _logger.LogInformation("Task {TaskId} was already processed, skipping", taskId);
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        
        await _cache.RemoveAsync(taskId, cancellationToken);

        return true;
    }

    public Task<IReadOnlyList<Guid>> GetUnprocessedTaskIdsAsync(CancellationToken cancellationToken = default) =>
        _tasks.GetUnprocessedIdsAsync(cancellationToken);
}
