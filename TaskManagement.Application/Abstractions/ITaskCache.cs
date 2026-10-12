using TaskManagement.Application.Tasks;

namespace TaskManagement.Application.Abstractions;

public interface ITaskCache
{
    Task<TaskResponse?> GetAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task SetAsync(TaskResponse task, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid taskId, CancellationToken cancellationToken = default);
}
