using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Defects gain a sequential Number (rendered "DEF-0001"), which doubles as the mailbox tag
    // stem ("JPMS/DEF-0001") — the Control Centre can now file subcontractor emails to a defect,
    // and the defect reads its mail back live by that tag like every other linkable record.
    // Numbering is GLOBAL (like to-do and work-order numbers) because all tags share one flat
    // category space. Existing rows are backfilled in RaisedAt order so every defect has a stable
    // reference before the first email is ever tagged. Expand-first safe: the column is additive
    // and old code never reads it.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260807160000_AddDefectNumbers")]
    public partial class AddDefectNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Defects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: one global sequence in RaisedAt order (DefectId tiebreak so the numbering
            // is deterministic when two defects share a timestamp).
            migrationBuilder.Sql(@"
WITH numbered AS (
    SELECT DefectId, ROW_NUMBER() OVER (ORDER BY RaisedAt, DefectId) AS rn
    FROM Defects
)
UPDATE d SET d.Number = n.rn
FROM Defects d
INNER JOIN numbered n ON n.DefectId = d.DefectId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "Defects");
        }
    }
}
