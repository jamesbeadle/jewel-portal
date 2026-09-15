-- ============================================================================
-- AddEstimateBreakdown  (2026-09-15)
-- ============================================================================
-- The estimate's priced breakdown and client-facing narrative (the estimate
-- document in the tender's shape): LeadEstimateLines — one row per priced
-- line carrying its section (name, order, provisional), a cost code, quantity
-- × unit price = total — and, on LeadEstimates, the executive summary, build
-- time and exclusions the document prints. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260915140000_AddEstimateBreakdown.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the new columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-estimate-breakdown.sql -b -o add-estimate-breakdown.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260915140000_AddEstimateBreakdown')
BEGIN
    IF COL_LENGTH(N'[LeadEstimates]', N'ExecutiveSummary') IS NULL
        ALTER TABLE [LeadEstimates] ADD [ExecutiveSummary] nvarchar(max) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'[LeadEstimates]', N'BuildTime') IS NULL
        ALTER TABLE [LeadEstimates] ADD [BuildTime] nvarchar(1024) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'[LeadEstimates]', N'Exclusions') IS NULL
        ALTER TABLE [LeadEstimates] ADD [Exclusions] nvarchar(4000) NOT NULL DEFAULT N'';

    IF OBJECT_ID(N'[LeadEstimateLines]', N'U') IS NULL
    BEGIN
        CREATE TABLE [LeadEstimateLines] (
            [LineId] nvarchar(64) NOT NULL,
            [EstimateId] nvarchar(64) NOT NULL,
            [Section] nvarchar(256) NOT NULL,
            [SectionOrder] int NOT NULL,
            [SectionProvisional] bit NOT NULL,
            [CostCode] nvarchar(32) NOT NULL,
            [Description] nvarchar(1024) NOT NULL,
            [Quantity] decimal(18,4) NOT NULL,
            [Unit] nvarchar(32) NOT NULL,
            [UnitPrice] decimal(18,4) NOT NULL,
            [Total] decimal(18,4) NOT NULL,
            [SortOrder] int NOT NULL,
            CONSTRAINT [PK_LeadEstimateLines] PRIMARY KEY ([LineId])
        );
        CREATE INDEX [IX_LeadEstimateLines_EstimateId] ON [LeadEstimateLines] ([EstimateId]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260915140000_AddEstimateBreakdown')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915140000_AddEstimateBreakdown', N'8.0.10');
END;
GO

COMMIT;
GO
