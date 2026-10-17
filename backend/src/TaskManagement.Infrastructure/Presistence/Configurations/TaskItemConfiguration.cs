using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Users;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title).HasMaxLength(TaskItem.TitleMaxLength).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(TaskItem.DescriptionMaxLength);
        // Concurrency token: EF adds "AND Status = <value when loaded>" to every UPDATE of a task.
        // Without it, two status changes arriving together could both pass the "Done is final"
        // check and the last one would overwrite a Done task. Now the second one updates no row
        // and fails with DbUpdateConcurrencyException, which is reported as 409.
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired().IsConcurrencyToken();
        // Priority stays numeric so the database can sort by it (High = 3 first).
        builder.Property(t => t.Priority).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backs the "no duplicate title on the same day for the same user" rule, so two
        // concurrent requests cannot both pass the check in the application service.
        // The default SQL Server collation is case-insensitive, so "Report" equals "report".
        builder.HasIndex(t => new { t.UserId, t.CreatedOn, t.Title }).IsUnique();

        builder.Ignore(t => t.IsProcessed);
    }
}
