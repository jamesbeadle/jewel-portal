using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // RFI numbering refactor — the database becomes the source of truth for reference uniqueness.
    // References (e.g. "RFI-012") are project-scoped: every project runs its own RFI sequence and the
    // client only ever sees the project-local number, so the same reference legitimately appears on
    // many projects but never twice on one. Until now that was enforced only by an application-level
    // check (RequestReferenceGuard), which races under concurrent writes. This index is the backstop:
    // a duplicate insert that slips past the guard now fails at the database, and the handlers turn
    // that failure into either a re-mint (auto-numbered) or a friendly error (manually typed).
    //
    // The index is filtered to non-blank references: a blank reference is a validation concern, not a
    // uniqueness one. SQL Server's default case-insensitive collation gives the same case-insensitive
    // semantics as the guard ("rfi-012" clashes with "RFI-012"). As a composite starting on ProjectId
    // it also serves as the register's per-project lookup index, which the table previously lacked.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260702120000_AddRequestReferenceUniqueIndex")]
    public partial class AddRequestReferenceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defensive pre-step: if any project already carries duplicate references (possible under
            // the old race), suffix the later-raised rows -DUP1, -DUP2… so the unique index can always
            // be created. The earliest-raised row keeps the original number. A clean database (the
            // expected case — the live By France register is already unique) is untouched.
            migrationBuilder.Sql(@"
WITH numbered AS (
    SELECT RequestId,
           ROW_NUMBER() OVER (PARTITION BY ProjectId, UPPER(Reference)
                              ORDER BY RaisedAt, RequestId) AS rn
    FROM Requests
    WHERE Reference <> N''
)
UPDATE r
SET r.Reference = r.Reference + N'-DUP' + CAST(n.rn - 1 AS NVARCHAR(8))
FROM Requests r
INNER JOIN numbered n ON n.RequestId = r.RequestId
WHERE n.rn > 1;");

            migrationBuilder.CreateIndex(
                name: "UX_Requests_Project_Reference",
                table: "Requests",
                columns: new[] { "ProjectId", "Reference" },
                unique: true,
                filter: "[Reference] <> N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "UX_Requests_Project_Reference", table: "Requests");
        }
    }
}
