using System.Text.RegularExpressions;
using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks;

// Named TaskItem to avoid clashing with System.Threading.Tasks.Task.
public class TaskItem : Entity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    // Characters that take no space on screen (zero-width space, word joiner, byte order mark).
    // Left in, "fixbug" and "fix\u200Bbug" would look the same on screen but count as different titles.
    private static readonly Regex InvisibleCharacters = new("[\u200B\u2060\uFEFF]", RegexOptions.Compiled);

    // Any run of whitespace: spaces, tabs, line breaks, non-breaking spaces and other Unicode spaces.
    private static readonly Regex WhitespaceRuns = new(@"\s+", RegexOptions.Compiled);

    private TaskItem()
    {
    }

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>The UTC calendar day the task was created on. Used by the "no duplicate title per day" rule.</summary>
    public DateOnly CreatedOn { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Set by the background worker once it has processed the task. The worker writes it with a
    /// single database UPDATE (ITaskRepository.MarkProcessedAsync) that only matches an
    /// unprocessed task, so it is set once and never touches the user's status.
    /// </summary>
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

    /// <summary>
    /// Stores the title the way it looks on screen: invisible characters removed, every run of
    /// whitespace turned into one space, and the ends trimmed. So " Fix  bug", "Fix\tbug" and
    /// "Fix bug" are the same title, and the duplicate rule and its unique index treat them so.
    /// </summary>
    public static string NormalizeTitle(string? title)
    {
        title = WhitespaceRuns.Replace(InvisibleCharacters.Replace(title ?? string.Empty, string.Empty), " ").Trim();

        // Checked after normalizing, so a title made only of spaces or invisible characters is rejected too.
        if (title.Length == 0)
            throw new DomainException("Title is required.");

        if (title.Length > TitleMaxLength)
            throw new DomainException($"Title cannot be longer than {TitleMaxLength} characters.");

        return title;
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    /// <summary>
    /// Changes the status. Done is a final state, and setting the current status again is a no-op.
    /// </summary>
    /// <returns>True if the status actually changed.</returns>
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
}
