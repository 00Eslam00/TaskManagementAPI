using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.UnitTests.Infrastructure;

/// <summary>
/// Checks the EF Core model itself. Building the model needs no database connection,
/// so these tests run without SQL Server.
/// </summary>
public class AppDbContextModelTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options);

    [Fact]
    public void Task_status_is_a_concurrency_token()
    {
        using var context = CreateContext();

        var status = context.Model.FindEntityType(typeof(TaskItem))!.FindProperty(nameof(TaskItem.Status))!;

        // Makes EF add "AND Status = <original>" to UPDATEs, so two simultaneous status changes
        // cannot both succeed and reopen a Done task.
        Assert.True(status.IsConcurrencyToken);
    }
}
