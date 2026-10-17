using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks;

public class TaskProcessor : ITaskProcessor
{
    private readonly ITaskRepository _tasks;
    private readonly ITaskCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TaskProcessor> _logger;

    public TaskProcessor(
        ITaskRepository tasks,
        ITaskCache cache,
        TimeProvider timeProvider,
        ILogger<TaskProcessor> logger)
    {
        _tasks = tasks;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        // One atomic UPDATE instead of load, change, save. Loading the task and saving it back
        // would make the save fail whenever the user changes the status in between (Status is a
        // concurrency token), leaving the task unprocessed until the next restart.
        var marked = await _tasks.MarkProcessedAsync(taskId, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (!marked)
        {
            // The task no longer exists, or it was already processed (for example queued twice).
            _logger.LogInformation("Task {TaskId} was already processed or no longer exists, skipping", taskId);
            return false;
        }

        // The cached copy still has no ProcessedAt, so it must be invalidated here as well.
        await _cache.RemoveAsync(taskId, cancellationToken);

        return true;
    }

    public Task<IReadOnlyList<Guid>> GetUnprocessedTaskIdsAsync(CancellationToken cancellationToken = default) =>
        _tasks.GetUnprocessedIdsAsync(cancellationToken);
}
