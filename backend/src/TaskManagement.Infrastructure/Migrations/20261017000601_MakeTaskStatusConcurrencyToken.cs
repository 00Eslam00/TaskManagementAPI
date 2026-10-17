using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Marks Tasks.Status as a concurrency token. This only changes how EF Core writes UPDATE
    /// statements (it adds "AND Status = @original"), not the table, so there is nothing to run.
    /// The migration exists to keep the model snapshot in step with the model.
    /// </summary>
    public partial class MakeTaskStatusConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
