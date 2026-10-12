namespace TaskManagement.Application.Tasks;

public interface ITaskProcessor
{
    Task<bool> ProcessAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetUnprocessedTaskIdsAsync(CancellationToken cancellationToken = default);
}
