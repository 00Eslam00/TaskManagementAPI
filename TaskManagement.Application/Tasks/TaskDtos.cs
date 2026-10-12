using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks;

public class CreateTaskRequest
{
    [Required, MaxLength(TaskItem.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; set; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
}

public class UpdateTaskStatusRequest
{
    [EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus Status { get; set; }
}

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime CreatedAt,
    Guid UserId,
    DateTime? ProcessedAt);

public static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.Priority, task.CreatedAt, task.UserId, task.ProcessedAt);
}
