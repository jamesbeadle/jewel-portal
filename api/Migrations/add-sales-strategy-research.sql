-- ============================================================================
-- AddSalesStrategyResearch  (2026-09-06)
-- ============================================================================
-- The strategy brief (the idea in the team's own words) and the AI research
-- state + findings — Claude reads the brief, searches the web and fills the
-- strategy in from the worker. Additive columns on SalesStrategies only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260906160000_AddSalesStrategyResearch.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the new columns. Every column guarded on its own.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-sales-strategy-research.sql -b -o add-sales-strategy-research.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906160000_AddSalesStrategyResearch')
BEGIN
    IF COL_LENGTH('SalesStrategies', 'Brief') IS NULL
        ALTER TABLE [SalesStrategies] ADD [Brief] nvarchar(4000) NOT NULL CONSTRAINT [DF_SalesStrategies_Brief] DEFAULT N'';
    IF COL_LENGTH('SalesStrategies', 'ResearchStatus') IS NULL
        ALTER TABLE [SalesStrategies] ADD [ResearchStatus] int NOT NULL CONSTRAINT [DF_SalesStrategies_ResearchStatus] DEFAULT 0;
    IF COL_LENGTH('SalesStrategies', 'ResearchRequestedAt') IS NULL
        ALTER TABLE [SalesStrategies] ADD [ResearchRequestedAt] datetimeoffset NULL;
    IF COL_LENGTH('SalesStrategies', 'ResearchCompletedAt') IS NULL
        ALTER TABLE [SalesStrategies] ADD [ResearchCompletedAt] datetimeoffset NULL;
    IF COL_LENGTH('SalesStrategies', 'ResearchError') IS NULL
        ALTER TABLE [SalesStrategies] ADD [ResearchError] nvarchar(2000) NULL;
    IF COL_LENGTH('SalesStrategies', 'ResearchFindings') IS NULL
        ALTER TABLE [SalesStrategies] ADD [ResearchFindings] nvarchar(max) NOT NULL CONSTRAINT [DF_SalesStrategies_ResearchFindings] DEFAULT N'';
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906160000_AddSalesStrategyResearch')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906160000_AddSalesStrategyResearch', N'8.0.10');
END;
GO

COMMIT;
GO
