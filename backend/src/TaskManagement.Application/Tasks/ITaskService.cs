namespace TaskManagement.Application.Tasks;

/// <summary>
/// Every method takes the id of the calling user and only ever touches that user's own tasks.
/// A task that belongs to someone else is reported as not found.
/// </summary>
public interface ITaskService
{
    Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Returns the user's tasks ordered by priority (highest first), then by creation date (oldest first).</summary>
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateStatusAsync(Guid userId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
}
