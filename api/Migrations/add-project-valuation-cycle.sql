-- ============================================================================
-- AddProjectValuationCycle  (2026-09-24)
-- ============================================================================
-- How often a project is valued, from its contract terms: Projects.ValuationCycle
-- (0 = not set, 1 = every 2 weeks, 2 = every 4 weeks, 3 = monthly). Every existing
-- project gets 0, so its next valuation date reads exactly as it does today until
-- a cycle is picked in Project settings. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260924200000_AddProjectValuationCycle.cs.
-- Must be applied BEFORE or WITH the deploy: the deployed api reads the column.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-project-valuation-cycle.sql -b -o add-project-valuation-cycle.log
-- ============================================================================

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924200000_AddProjectValuationCycle')
BEGIN
    IF COL_LENGTH(N'[Projects]', N'ValuationCycle') IS NULL
        ALTER TABLE [Projects] ADD [ValuationCycle] int NOT NULL CONSTRAINT [DF_Projects_ValuationCycle] DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924200000_AddProjectValuationCycle')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924200000_AddProjectValuationCycle', N'8.0.10');
END;
GO

COMMIT;
GO
