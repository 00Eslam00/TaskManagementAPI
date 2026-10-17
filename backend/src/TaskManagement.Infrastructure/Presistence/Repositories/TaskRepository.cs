using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsWithTitleOnDayAsync(Guid userId, string normalizedTitle, DateOnly day, CancellationToken cancellationToken = default) =>
        _context.Tasks.AnyAsync(t => t.UserId == userId && t.CreatedOn == day && t.Title == normalizedTitle, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetUnprocessedIdsAsync(CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .Where(t => t.ProcessedAt == null)
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

    // ExecuteUpdate sends one "UPDATE ... WHERE Id = @id AND ProcessedAt IS NULL" straight to the
    // database, without loading the task. Because the entity is never tracked, the Status
    // concurrency token is not part of the WHERE clause, and "processed only once" is decided by
    // the database: if two workers race, only one UPDATE matches the row.
    public async Task<bool> MarkProcessedAsync(Guid id, DateTime processedAt, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .Where(t => t.Id == id && t.ProcessedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ProcessedAt, processedAt), cancellationToken) == 1;

    public void Add(TaskItem task) => _context.Tasks.Add(task);
}
