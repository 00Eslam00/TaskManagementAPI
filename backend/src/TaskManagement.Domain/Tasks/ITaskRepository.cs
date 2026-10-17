namespace TaskManagement.Domain.Tasks;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the user's tasks ordered by priority (highest first), then by creation date (oldest first).</summary>
    Task<IReadOnlyList<TaskItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithTitleOnDayAsync(Guid userId, string normalizedTitle, DateOnly day, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetUnprocessedIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets ProcessedAt in a single UPDATE, only if the task exists and was not processed yet.
    /// It writes no other column and does not check the status, so it never conflicts with,
    /// or overwrites, a status change the user makes at the same time.
    /// </summary>
    /// <returns>True if this call marked the task as processed.</returns>
    Task<bool> MarkProcessedAsync(Guid id, DateTime processedAt, CancellationToken cancellationToken = default);

    void Add(TaskItem task);
}
