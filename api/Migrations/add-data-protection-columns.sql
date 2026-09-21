-- ============================================================================
-- AddDataProtectionColumns  (2026-09-21)
-- ============================================================================
-- The data-protection task: Leads.MarketingConsentGivenAt / MarketingConsentWithdrawnAt
-- (whether the prospect said we may keep in touch, and when they asked us to
-- stop — read by every follow-up send), Workers.RetiredAt (stamped by RetireWorker
-- when a worker's contact details are cleared), and an index on
-- AuditEvents.OccurredAt so the nightly retention sweep can retire audit rows by
-- age without scanning the table. NULL for every existing row. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260921180000_AddDataProtectionColumns.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i add-data-protection-columns.sql -b -o add-data-protection-columns.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260921180000_AddDataProtectionColumns')
BEGIN
    IF COL_LENGTH(N'[Leads]', N'MarketingConsentGivenAt') IS NULL
        ALTER TABLE [Leads] ADD [MarketingConsentGivenAt] datetimeoffset NULL;
    IF COL_LENGTH(N'[Leads]', N'MarketingConsentWithdrawnAt') IS NULL
        ALTER TABLE [Leads] ADD [MarketingConsentWithdrawnAt] datetimeoffset NULL;
    IF COL_LENGTH(N'[Workers]', N'RetiredAt') IS NULL
        ALTER TABLE [Workers] ADD [RetiredAt] datetimeoffset NULL;
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AuditEvents_OccurredAt' AND object_id = OBJECT_ID(N'[AuditEvents]'))
        CREATE INDEX [IX_AuditEvents_OccurredAt] ON [AuditEvents] ([OccurredAt]);
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260921180000_AddDataProtectionColumns')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260921180000_AddDataProtectionColumns', N'8.0.10');
END;
GO

COMMIT;
GO
