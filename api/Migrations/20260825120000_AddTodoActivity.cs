using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // The to-do timeline (2026-08-25, Jeremy's "how do I show I've sent it but it's not done"):
    // one TodoItemActivities row per change / logged chase / email sent from the item's page, plus
    // the In-progress stamp on TodoItems (StartedAt / StartedByEmail — null = still Open).
    // Purely additive; apply BEFORE the deploy, because the deployed code reads the new columns.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260825120000_AddTodoActivity")]
    public partial class AddTodoActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartedByEmail",
                table: "TodoItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TodoItemActivities",
                columns: table => new
                {
                    TodoItemActivityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TodoItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemActivities", x => x.TodoItemActivityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemActivities_TodoItemId",
                table: "TodoItemActivities",
                column: "TodoItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TodoItemActivities");
            migrationBuilder.DropColumn(name: "StartedByEmail", table: "TodoItems");
            migrationBuilder.DropColumn(name: "StartedAt", table: "TodoItems");
        }
    }
}
