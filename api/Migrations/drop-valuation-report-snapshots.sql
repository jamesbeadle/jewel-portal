-- ============================================================================
-- DropValuationReportSnapshots  (2026-09-18)  — step 2 of 2 (CONTRACT)
-- ============================================================================
-- Removes the retired valuation-report-snapshot object once step 1
-- (consolidate-valuation-statements.sql) has run and its printed
-- reconciliation has been checked: every locked claim's frozen lines sum to
-- its TotalWorksComplete, and no CLAIM-LESS SNAPSHOT row was listed.
--
-- Run this only AFTER the api that no longer reads these tables is deployed.
-- Nothing here is needed by the new code; the alias register
-- (ValuationClaimLegacyStatements) already carries every old id and VRS
-- number, and every live statement's lines already sit on its claim.
--
-- What goes: ValuationReportSnapshotLines, ValuationReportSnapshots and the
-- ValuationInvoices.ValuationReportSnapshotId column. Refuses to run while any
-- snapshot with a claim is missing from the alias register (step 1 not run).
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i drop-valuation-report-snapshots.sql -b -o drop-valuation-report-snapshots.log
-- ============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[ValuationReportSnapshots]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [ValuationReportSnapshots] s
               WHERE s.[ValuationClaimId] IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM [ValuationClaimLegacyStatements] l WHERE l.[ValuationReportSnapshotId] = s.[ValuationReportSnapshotId]))
BEGIN
    RAISERROR (N'Snapshots exist that are not in ValuationClaimLegacyStatements — run consolidate-valuation-statements.sql first.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;
GO

IF OBJECT_ID(N'[ValuationReportSnapshotLines]', N'U') IS NOT NULL DROP TABLE [ValuationReportSnapshotLines];
IF OBJECT_ID(N'[ValuationReportSnapshots]', N'U') IS NOT NULL DROP TABLE [ValuationReportSnapshots];
GO

IF COL_LENGTH(N'ValuationInvoices', N'ValuationReportSnapshotId') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[ValuationInvoices]') AND c.[name] = N'ValuationReportSnapshotId');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [ValuationInvoices] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [ValuationInvoices] DROP COLUMN [ValuationReportSnapshotId];
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260918130000_DropValuationReportSnapshots')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260918130000_DropValuationReportSnapshots', N'8.0.10');
END;
GO

COMMIT;
GO
