using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks;

public class TaskItem : Entity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem()
    {
    }

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public DateOnly CreatedOn { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public bool IsProcessed => ProcessedAt is not null;

    public static TaskItem Create(Guid userId, string title, string? description, TaskPriority priority, DateTime utcNow)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A task must belong to a user.");

        if (!Enum.IsDefined(priority))
            throw new DomainException("Priority is not valid.");

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description is { Length: > DescriptionMaxLength })
            throw new DomainException($"Description cannot be longer than {DescriptionMaxLength} characters.");

        return new TaskItem
        {
            UserId = userId,
            Title = NormalizeTitle(title),
            Description = description,
            Priority = priority,
            Status = TaskItemStatus.Pending,
            CreatedAt = utcNow,
            CreatedOn = DateOnly.FromDateTime(utcNow)
        };
    }

   
    public static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");

        title = title.Trim();
        if (title.Length > TitleMaxLength)
            throw new DomainException($"Title cannot be longer than {TitleMaxLength} characters.");

        return title;
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    public bool ChangeStatus(TaskItemStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
            throw new DomainException("Status is not valid.");

        if (newStatus == Status)
            return false;

        if (Status == TaskItemStatus.Done)
            throw new DomainException("A task that is Done cannot be changed.");

        Status = newStatus;
        return true;
    }

    public bool MarkProcessed(DateTime utcNow)
    {
        if (IsProcessed)
            return false;

        ProcessedAt = utcNow;
        return true;
    }
}
