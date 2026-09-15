-- ============================================================================
-- AddLeadEstimates  (2026-09-15)
-- ============================================================================
-- Estimates on a lead (the Sales pane): the LeadEstimates table — Jewel's own
-- pricing of one enquiry (scope, architect, price due date, budget mentioned,
-- total) with its status ladder Received → Pricing → Submitted → Won / Lost, a
-- global EST-#### number and the timestamps the moves stamp. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260915103000_AddLeadEstimates.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the new table.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-lead-estimates.sql -b -o add-lead-estimates.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260915103000_AddLeadEstimates')
BEGIN
    IF OBJECT_ID(N'[LeadEstimates]', N'U') IS NULL
    BEGIN
        CREATE TABLE [LeadEstimates] (
            [EstimateId] nvarchar(64) NOT NULL,
            [LeadId] nvarchar(64) NOT NULL,
            [Number] int NOT NULL,
            [Scope] nvarchar(4000) NOT NULL,
            [ArchitectName] nvarchar(256) NOT NULL,
            [PriceDueOn] date NULL,
            [BudgetMentioned] decimal(18,4) NULL,
            [Total] decimal(18,4) NULL,
            [Notes] nvarchar(4000) NOT NULL,
            [Status] int NOT NULL,
            [StatusChangedAt] datetimeoffset NOT NULL,
            [SubmittedAt] datetimeoffset NULL,
            [CreatedByEmail] nvarchar(256) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_LeadEstimates] PRIMARY KEY ([EstimateId])
        );
        CREATE INDEX [IX_LeadEstimates_LeadId] ON [LeadEstimates] ([LeadId]);
        CREATE INDEX [IX_LeadEstimates_Number] ON [LeadEstimates] ([Number]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260915103000_AddLeadEstimates')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915103000_AddLeadEstimates', N'8.0.10');
END;
GO

COMMIT;
GO
