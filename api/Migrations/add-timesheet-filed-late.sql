-- ============================================================================
-- AddTimesheetFiledLate  (2026-09-30)
-- ============================================================================
-- A day filed or changed by the worker after its date — a missed day filled in
-- from My day, a day amended the day after — is marked late for the office.
-- One bit column on Timesheets, false for every day already there. Additive only.
--
-- House-style scoped script: applies the migration directly and records its id
-- in __EFMigrationsHistory so EF never re-applies it. Mirrors
-- api/Migrations/20260930120000_AddTimesheetFiledLate.cs. Safe to apply BEFORE
-- or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-timesheet-filed-late.sql -b -o add-timesheet-filed-late.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260930120000_AddTimesheetFiledLate')
BEGIN
    IF COL_LENGTH(N'[Timesheets]', N'IsFiledLate') IS NULL
    ALTER TABLE [Timesheets] ADD [IsFiledLate] bit NOT NULL DEFAULT CAST(0 AS bit);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930120000_AddTimesheetFiledLate', N'8.0.10');
END;
GO

COMMIT;
GO
