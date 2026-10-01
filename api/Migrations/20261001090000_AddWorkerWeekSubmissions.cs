using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The weeks operatives submit from My day for the office to sign off in one step (2026-10-01,
    /// Jeremy's ask). One table, one row per worker and week, additive only. Script:
    /// add-worker-week-submissions.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20261001090000_AddWorkerWeekSubmissions")]
    public partial class AddWorkerWeekSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerWeekSubmissions",
                columns: table => new
                {
                    WorkerWeekSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WeekStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerWeekSubmissions", x => x.WorkerWeekSubmissionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerWeekSubmissions_WorkerId_WeekStart",
                table: "WorkerWeekSubmissions",
                columns: new[] { "WorkerId", "WeekStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerWeekSubmissions");
        }
    }
}
