-- ============================================================================
-- AddContractorsReportContactAndPhotoChoice  (2026-09-24)
-- ============================================================================
-- The Contractor's Report as issued Report 30 reads: an entered Building
-- Control contact line for a project with no Building Control case, and the
-- photographs Section 9 leaves out of a selected update. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors
-- api/Migrations/20260924180000_AddContractorsReportContactAndPhotoChoice.cs.
-- Apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-contractors-report-contact-and-photo-choice.sql -b \
--          -o add-contractors-report-contact-and-photo-choice.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924180000_AddContractorsReportContactAndPhotoChoice')
BEGIN
    IF COL_LENGTH(N'ContractorsReports', N'BuildingControlContact') IS NULL
        ALTER TABLE [ContractorsReports] ADD [BuildingControlContact] nvarchar(512) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'ContractorsReports', N'ExcludedPhotoIdsJson') IS NULL
        ALTER TABLE [ContractorsReports] ADD [ExcludedPhotoIdsJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924180000_AddContractorsReportContactAndPhotoChoice')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924180000_AddContractorsReportContactAndPhotoChoice', N'8.0.10');
END;
GO

COMMIT;
GO
