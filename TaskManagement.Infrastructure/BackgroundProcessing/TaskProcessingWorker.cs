using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Tasks;

namespace TaskManagement.Infrastructure.BackgroundProcessing;


// Long-running worker that takes task ids from the queue, simulates some work,
// and then marks each task as processed.

public class TaskProcessingWorker : BackgroundService
{
    private readonly TaskProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundProcessingOptions _options;
    private readonly ILogger<TaskProcessingWorker> _logger;

    public TaskProcessingWorker(
        TaskProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundProcessingOptions> options,
        ILogger<TaskProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task processing worker started");

        try
        {
            await RequeueUnprocessedTasksAsync(stoppingToken);

            await foreach (var taskId in _queue.DequeueAllAsync(stoppingToken))
            {
                await ProcessAsync(taskId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down.
        }

        _logger.LogInformation("Task processing worker stopped");
    }


    // The queue lives in memory, so anything still waiting in it is lost when the application
    // stops. On startup, tasks that were never processed are put back in the queue.
    private async Task RequeueUnprocessedTasksAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ITaskProcessor>();

            var taskIds = await processor.GetUnprocessedTaskIdsAsync(stoppingToken);
            foreach (var taskId in taskIds)
                await _queue.EnqueueAsync(taskId, stoppingToken);

            if (taskIds.Count > 0)
                _logger.LogInformation("Re-queued {Count} unprocessed task(s) from a previous run", taskIds.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not re-queue unprocessed tasks");
        }
    }

    private async Task ProcessAsync(Guid taskId, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing task {TaskId}", taskId);

            // Simulate some work.
            await Task.Delay(TimeSpan.FromSeconds(_options.SimulatedWorkSeconds), stoppingToken);

            // The worker is a singleton, but repositories and the DbContext are scoped,
            // so each task gets its own scope, just like each HTTP request does.

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ITaskProcessor>();

            if (await processor.ProcessAsync(taskId, stoppingToken))
                _logger.LogInformation("Task {TaskId} processed", taskId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One failing task stays unprocessed in the database
            // and is picked up again on the next startup.
            _logger.LogError(ex, "Failed to process task {TaskId}", taskId);
        }
    }
}
