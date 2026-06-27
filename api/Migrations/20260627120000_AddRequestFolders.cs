using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MailboxFolderId",
                table: "Requests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            // Backfill sequential numbers for existing requests so their folder names (REQ-0001…)
            // are stable and ordered by when they were raised. New rows take MAX(Number)+1 at insert.
            migrationBuilder.Sql(@"
WITH numbered AS (
    SELECT RequestId,
           ROW_NUMBER() OVER (ORDER BY RaisedAt, RequestId) AS rn
    FROM Requests
)
UPDATE r
SET r.Number = n.rn
FROM Requests r
INNER JOIN numbered n ON n.RequestId = r.RequestId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MailboxFolderId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Requests");
        }
    }
}
