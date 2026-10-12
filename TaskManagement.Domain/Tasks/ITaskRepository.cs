namespace TaskManagement.Domain.Tasks;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the user's tasks ordered by priority (highest first), then by creation date (oldest first).</summary>
    Task<IReadOnlyList<TaskItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithTitleOnDayAsync(Guid userId, string normalizedTitle, DateOnly day, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetUnprocessedIdsAsync(CancellationToken cancellationToken = default);

    void Add(TaskItem task);
}
