using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Step 2 of 2 of consolidating valuation snapshots into the claim (2026-09-18): drops the
    /// retired ValuationReportSnapshots / ValuationReportSnapshotLines tables and the
    /// ValuationInvoices.ValuationReportSnapshotId column. Run only after step 1's backfill has
    /// been checked and the api that no longer reads them is deployed. Scoped script (which also
    /// refuses to run while any snapshot is missing from the alias register):
    /// api/Migrations/drop-valuation-report-snapshots.sql. Not reversible — the data now lives on
    /// the claims and in ValuationClaimLegacyStatements.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260918130000_DropValuationReportSnapshots")]
    public partial class DropValuationReportSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[ValuationReportSnapshotLines]', N'U') IS NOT NULL DROP TABLE [ValuationReportSnapshotLines];");
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[ValuationReportSnapshots]', N'U') IS NOT NULL DROP TABLE [ValuationReportSnapshots];");
            migrationBuilder.Sql(@"IF COL_LENGTH(N'ValuationInvoices', N'ValuationReportSnapshotId') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[ValuationInvoices]') AND c.[name] = N'ValuationReportSnapshotId');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [ValuationInvoices] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [ValuationInvoices] DROP COLUMN [ValuationReportSnapshotId];
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("The valuation snapshot tables cannot be recreated — their data lives on the claims and in ValuationClaimLegacyStatements.");
        }
    }
}
