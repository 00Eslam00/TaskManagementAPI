using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.UnitTests.Domain;

public class TaskItemTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 23, 30, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    private static TaskItem NewTask(string title = "Write report") =>
        TaskItem.Create(UserId, title, "Quarterly numbers", TaskPriority.High, Now);

    [Fact]
    public void Create_sets_defaults_and_trims_title()
    {
        var task = NewTask("  Write report  ");

        Assert.Equal("Write report", task.Title);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(new DateOnly(2026, 1, 15), task.CreatedOn);
        Assert.Equal(UserId, task.UserId);
        Assert.False(task.IsProcessed);
    }

    [Theory]
    [InlineData("Write   report")]          // several spaces
    [InlineData("Write\treport")]           // tab
    [InlineData("Write\r\nreport")]         // line break
    [InlineData("Write\u00A0report")]       // non-breaking space
    [InlineData(" \u200BWrite  report\uFEFF ")] // invisible characters and spaces around
    public void Create_stores_the_title_as_it_looks_on_screen(string title)
    {
        // All of these render as "Write report", so they must be stored as exactly that;
        // otherwise each would pass the "no duplicate title on the same day" rule.
        Assert.Equal("Write report", NewTask(title).Title);
    }

    [Fact]
    public void Create_removes_an_invisible_character_inside_a_word()
    {
        Assert.Equal("Writereport", NewTask("Write\u200Breport").Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \u00A0")]
    [InlineData("\u200B\uFEFF")]
    public void Create_rejects_empty_title(string title)
    {
        Assert.Throws<DomainException>(() => NewTask(title));
    }

    [Fact]
    public void Create_checks_the_length_after_collapsing_spaces()
    {
        // 200 visible characters plus extra spaces in the middle is still a valid title.
        var title = new string('a', 100) + "     " + new string('b', 99);

        Assert.Equal(TaskItem.TitleMaxLength, NewTask(title).Title.Length);
    }

    [Fact]
    public void Create_rejects_title_that_is_too_long()
    {
        Assert.Throws<DomainException>(() => NewTask(new string('a', TaskItem.TitleMaxLength + 1)));
    }

    [Fact]
    public void Create_rejects_missing_owner()
    {
        Assert.Throws<DomainException>(() => TaskItem.Create(Guid.Empty, "Title", null, TaskPriority.Low, Now));
    }

    [Fact]
    public void Create_rejects_unknown_priority()
    {
        Assert.Throws<DomainException>(() => TaskItem.Create(UserId, "Title", null, (TaskPriority)99, Now));
    }

    [Fact]
    public void ChangeStatus_moves_to_new_status()
    {
        var task = NewTask();

        Assert.True(task.ChangeStatus(TaskItemStatus.InProgress));
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
    }

    [Fact]
    public void ChangeStatus_to_same_status_is_a_no_op()
    {
        var task = NewTask();

        Assert.False(task.ChangeStatus(TaskItemStatus.Pending));
        Assert.Equal(TaskItemStatus.Pending, task.Status);
    }

    [Fact]
    public void ChangeStatus_rejects_changing_a_done_task()
    {
        var task = NewTask();
        task.ChangeStatus(TaskItemStatus.Done);

        Assert.Throws<DomainException>(() => task.ChangeStatus(TaskItemStatus.InProgress));
    }

    [Fact]
    public void ChangeStatus_rejects_unknown_status()
    {
        Assert.Throws<DomainException>(() => NewTask().ChangeStatus((TaskItemStatus)99));
    }

    [Fact]
    public void IsOwnedBy_is_true_only_for_the_owner()
    {
        var task = NewTask();

        Assert.True(task.IsOwnedBy(UserId));
        Assert.False(task.IsOwnedBy(Guid.NewGuid()));
    }
}
