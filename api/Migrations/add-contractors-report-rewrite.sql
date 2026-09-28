-- ============================================================================
-- AddContractorsReportRewrite  (2026-09-28)
-- ============================================================================
-- The Contractor's Report's rewrite: the week's daily logs rewritten in house
-- language with the flag list the office clears before the build, one JSON
-- column beside the report's other JSON lists, null until asked for.
-- Additive only: one nullable column, no data changes.
--
-- House-style scoped script, as add-site-log-raised-records.sql: applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260928223551_AddContractorsReportRewrite.cs.
-- Apply BEFORE or WITH the deploy. Safe to run twice.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-contractors-report-rewrite.sql -b -o add-contractors-report-rewrite.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928223551_AddContractorsReportRewrite'
)
BEGIN
    IF COL_LENGTH(N'ContractorsReports', N'RewriteJson') IS NULL
        ALTER TABLE [ContractorsReports] ADD [RewriteJson] nvarchar(max) NULL;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928223551_AddContractorsReportRewrite', N'8.0.10');
END;
GO

COMMIT;
GO
