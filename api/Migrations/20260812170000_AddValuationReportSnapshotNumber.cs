using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Valuation report snapshots become a linkable record type in mailbox triage: an email is
    // associated with a specific snapshot by the tag "JPMS/VRS-{projectRef}-{Number}". Snapshots
    // had no reference of their own (GUID id + free-text label), so:
    //  - ValuationReportSnapshots gains Number — a per-project sequential minted at capture
    //    (max + 1). Persisted, never derived from register order: tags already stamped on
    //    emails in Outlook must not drift when a snapshot is deleted.
    //  - Existing rows are backfilled per project in TakenAt order, so every snapshot on the
    //    register is immediately linkable.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260812170000_AddValuationReportSnapshotNumber")]
    public partial class AddValuationReportSnapshotNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number", table: "ValuationReportSnapshots", type: "int",
                nullable: false, defaultValue: 0);

            // Backfill: number existing snapshots per project in capture order (TakenAt, id as
            // the tiebreak). Wrapped in sp_executesql so this batch still COMPILES if a later
            // migration ever reshapes the table — inline raw SQL referencing dropped columns is
            // what permanently poisoned the full idempotent script (see CLAUDE.md).
            migrationBuilder.Sql(@"
EXEC sp_executesql N'
WITH numbered AS (
    SELECT ValuationReportSnapshotId,
           ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY TakenAt, ValuationReportSnapshotId) AS rn
    FROM ValuationReportSnapshots
)
UPDATE s
SET s.Number = n.rn
FROM ValuationReportSnapshots s
INNER JOIN numbered n ON n.ValuationReportSnapshotId = s.ValuationReportSnapshotId
WHERE s.Number = 0;';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Number", table: "ValuationReportSnapshots");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
