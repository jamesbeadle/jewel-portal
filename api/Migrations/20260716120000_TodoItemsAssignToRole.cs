using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// To-do items are now assigned to a ROLE, not a person. Assigning to a named user broke the
    /// moment that user left: their open items pointed at a dead email address and silently
    /// disappeared from every dashboard. Assigning to the role instead means whoever currently
    /// holds it sees the items — a new starter taking over the role inherits the open list with
    /// no re-assignment.
    ///
    /// Existing rows are carried across by mapping each AssigneeEmail to the best to-do-assignable
    /// role that user holds in the directory (the most senior specific role wins; Admin only when
    /// it's the user's sole assignable role — an item meant for "the PM who is also an admin"
    /// should follow the PM role, not the admin hat). Items whose assignee has no directory row or
    /// no assignable role become unassigned (NULL) — exactly the rows that were already orphaned.
    /// The role ints mirror Models.Role: 0 Admin, 1 ManagingDirector, 2 FinanceDirector,
    /// 3 ProjectManager, 4 QuantitySurveyor, 5 SiteManager, 6 HealthSafetyOfficer,
    /// 7 OfficeComplianceCoordinator (8+ are not assignable).
    /// </summary>
    public partial class TodoItemsAssignToRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssigneeRole",
                table: "TodoItems",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE t
SET AssigneeRole = best.Role
FROM TodoItems t
CROSS APPLY (
    SELECT TOP 1 dur.Role
    FROM DirectoryUserRoles dur
    WHERE LOWER(dur.DirectoryUserEmail) = LOWER(t.AssigneeEmail)
      AND dur.Role IN (0, 1, 2, 3, 4, 5, 6, 7)
    ORDER BY CASE WHEN dur.Role = 0 THEN 99 ELSE dur.Role END
) best
WHERE t.AssigneeEmail <> '';
");

            migrationBuilder.DropColumn(
                name: "AssigneeEmail",
                table: "TodoItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The email -> role mapping is one-way; rolling back restores the column but items
            // come back unassigned.
            migrationBuilder.AddColumn<string>(
                name: "AssigneeEmail",
                table: "TodoItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "AssigneeRole",
                table: "TodoItems");
        }
    }
}
