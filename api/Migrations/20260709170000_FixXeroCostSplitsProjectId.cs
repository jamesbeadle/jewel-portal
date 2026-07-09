using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Corrective migration. 20260709120000_AddXeroCostSplitsAndWriteBack was edited
    // in place after it had already been applied to the live database (the edit added
    // ProjectId to XeroCostSplits, widened the PK from nvarchar(180) to nvarchar(256),
    // and re-keyed the cost-centre index). EF skips migrations whose ID is already in
    // __EFMigrationsHistory, so databases migrated from the original version are left
    // without the ProjectId column — which makes every query that selects
    // XeroCostSplits.ProjectId fail at runtime (notably GetProjectFinancialSummary,
    // zeroing the Financials tab).
    //
    // Every step is guarded so this runs cleanly against both populations:
    //  * databases that applied the ORIGINAL migration (no ProjectId, 180-wide PK,
    //    IX_XeroCostSplits_CostCenterCode) — everything below applies;
    //  * databases that applied the EDITED migration (fresh installs) — every guard
    //    is a no-op.
    //
    // Existing split rows (created before splits carried their own project) are
    // backfilled from their parent ledger line's ProjectId.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260709170000_FixXeroCostSplitsProjectId")]
    public partial class FixXeroCostSplitsProjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add ProjectId if missing, backfilled from the parent ledger line.
            migrationBuilder.Sql(
@"IF COL_LENGTH(N'XeroCostSplits', N'ProjectId') IS NULL
BEGIN
    ALTER TABLE [XeroCostSplits]
        ADD [ProjectId] nvarchar(64) NOT NULL CONSTRAINT [DF_XeroCostSplits_ProjectId] DEFAULT N'';

    EXEC(N'UPDATE splits
           SET splits.[ProjectId] = ISNULL(lines.[ProjectId], N'''')
           FROM [XeroCostSplits] splits
           JOIN [XeroLedgerLines] lines ON lines.[XeroLedgerLineId] = splits.[XeroLedgerLineId]');

    ALTER TABLE [XeroCostSplits] DROP CONSTRAINT [DF_XeroCostSplits_ProjectId];
END");

            // 2. Widen the PK column from nvarchar(180) to nvarchar(256) — split ids are
            //    ""{lineId}:{projectId}:{costCode}"" and can exceed 180. Requires the PK
            //    to be dropped and re-created around the ALTER.
            migrationBuilder.Sql(
@"IF COL_LENGTH(N'XeroCostSplits', N'XeroCostSplitId') < 512 -- COL_LENGTH is bytes; nvarchar(180) = 360
BEGIN
    ALTER TABLE [XeroCostSplits] DROP CONSTRAINT [PK_XeroCostSplits];
    ALTER TABLE [XeroCostSplits] ALTER COLUMN [XeroCostSplitId] nvarchar(256) NOT NULL;
    ALTER TABLE [XeroCostSplits] ADD CONSTRAINT [PK_XeroCostSplits] PRIMARY KEY ([XeroCostSplitId]);
END");

            // 3. Replace the cost-centre index with the (ProjectId, CostCenterCode) index.
            migrationBuilder.Sql(
@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_XeroCostSplits_CostCenterCode' AND object_id = OBJECT_ID(N'XeroCostSplits'))
    DROP INDEX [IX_XeroCostSplits_CostCenterCode] ON [XeroCostSplits];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_XeroCostSplits_ProjectId_CostCenterCode' AND object_id = OBJECT_ID(N'XeroCostSplits'))
    CREATE INDEX [IX_XeroCostSplits_ProjectId_CostCenterCode] ON [XeroCostSplits] ([ProjectId], [CostCenterCode]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverts to the ORIGINAL (as-applied) shape of 20260709120000.
            migrationBuilder.Sql(
@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_XeroCostSplits_ProjectId_CostCenterCode' AND object_id = OBJECT_ID(N'XeroCostSplits'))
    DROP INDEX [IX_XeroCostSplits_ProjectId_CostCenterCode] ON [XeroCostSplits];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_XeroCostSplits_CostCenterCode' AND object_id = OBJECT_ID(N'XeroCostSplits'))
    CREATE INDEX [IX_XeroCostSplits_CostCenterCode] ON [XeroCostSplits] ([CostCenterCode]);

IF COL_LENGTH(N'XeroCostSplits', N'ProjectId') IS NOT NULL
    EXEC(N'ALTER TABLE [XeroCostSplits] DROP COLUMN [ProjectId]');");
        }
    }
}
