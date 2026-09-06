-- ============================================================================
-- AddSalesStrategies  (2026-09-06)
-- ============================================================================
-- The Sales section: sales strategies (methodologies for finding leads, with
-- their justification and a Claude-drafted approach plan), each lead's
-- timeline, and the rebuilt lead register — new columns on Leads and a remap
-- of the May 2026 prototype's Stage / Source ints onto the shorter enums.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260906120000_AddSalesStrategies.cs.
--
-- Additive only (the prototype's six satellite CRM tables are left alone), so
-- it is safe to apply BEFORE or WITH the deploy — and it MUST be applied before
-- the deployed api queries the new Leads columns. Every step is guarded on
-- its own, on top of the history guard.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-sales-strategies.sql -b -o add-sales-strategies.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906120000_AddSalesStrategies'
)
BEGIN
    IF OBJECT_ID('SalesStrategies', 'U') IS NULL
    BEGIN
        CREATE TABLE [SalesStrategies] (
            [StrategyId] nvarchar(64) NOT NULL,
            [Name] nvarchar(256) NOT NULL,
            [Audience] int NOT NULL,
            [TargetArea] nvarchar(512) NOT NULL,
            [Hypothesis] nvarchar(4000) NOT NULL,
            [Evidence] nvarchar(4000) NOT NULL,
            [Channel] int NOT NULL,
            [Proposition] nvarchar(1024) NOT NULL,
            [ApproachPlan] nvarchar(max) NOT NULL,
            [PlanGeneratedAt] datetimeoffset NULL,
            [Status] int NOT NULL,
            [OwnerEmail] nvarchar(256) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [UpdatedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_SalesStrategies] PRIMARY KEY ([StrategyId])
        );
        CREATE INDEX [IX_SalesStrategies_Status] ON [SalesStrategies] ([Status]);
    END;

    IF OBJECT_ID('LeadActivities', 'U') IS NULL
    BEGIN
        CREATE TABLE [LeadActivities] (
            [LeadActivityId] nvarchar(64) NOT NULL,
            [LeadId] nvarchar(64) NOT NULL,
            [Kind] int NOT NULL,
            [Summary] nvarchar(4000) NOT NULL,
            [OccurredAt] datetimeoffset NOT NULL,
            [RecordedByEmail] nvarchar(256) NOT NULL,
            CONSTRAINT [PK_LeadActivities] PRIMARY KEY ([LeadActivityId])
        );
        CREATE INDEX [IX_LeadActivities_LeadId] ON [LeadActivities] ([LeadId]);
    END;
END;
GO

-- ---- Leads: the rebuild's columns (each guarded) ----
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906120000_AddSalesStrategies')
BEGIN
    IF COL_LENGTH('Leads', 'Number') IS NULL
        ALTER TABLE [Leads] ADD [Number] int NOT NULL CONSTRAINT [DF_Leads_Number] DEFAULT 0;
    IF COL_LENGTH('Leads', 'ProspectKind') IS NULL
        ALTER TABLE [Leads] ADD [ProspectKind] int NOT NULL CONSTRAINT [DF_Leads_ProspectKind] DEFAULT 0;
    IF COL_LENGTH('Leads', 'Postcode') IS NULL
        ALTER TABLE [Leads] ADD [Postcode] nvarchar(16) NOT NULL CONSTRAINT [DF_Leads_Postcode] DEFAULT N'';
    IF COL_LENGTH('Leads', 'Summary') IS NULL
        ALTER TABLE [Leads] ADD [Summary] nvarchar(512) NOT NULL CONSTRAINT [DF_Leads_Summary] DEFAULT N'';
    IF COL_LENGTH('Leads', 'Notes') IS NULL
        ALTER TABLE [Leads] ADD [Notes] nvarchar(4000) NOT NULL CONSTRAINT [DF_Leads_Notes] DEFAULT N'';
    IF COL_LENGTH('Leads', 'StrategyId') IS NULL
        ALTER TABLE [Leads] ADD [StrategyId] nvarchar(64) NULL;
    IF COL_LENGTH('Leads', 'StageChangedAt') IS NULL
        ALTER TABLE [Leads] ADD [StageChangedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Leads_StageChangedAt] DEFAULT '2026-01-01T00:00:00.0000000+00:00';
    IF COL_LENGTH('Leads', 'ClientId') IS NULL
        ALTER TABLE [Leads] ADD [ClientId] nvarchar(64) NULL;
    IF COL_LENGTH('Leads', 'ProjectId') IS NULL
        ALTER TABLE [Leads] ADD [ProjectId] nvarchar(64) NULL;
    IF COL_LENGTH('Leads', 'LostReason') IS NULL
        ALTER TABLE [Leads] ADD [LostReason] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906120000_AddSalesStrategies')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Leads_StrategyId' AND object_id = OBJECT_ID('Leads'))
        CREATE INDEX [IX_Leads_StrategyId] ON [Leads] ([StrategyId]);
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Leads_Number' AND object_id = OBJECT_ID('Leads'))
        CREATE INDEX [IX_Leads_Number] ON [Leads] ([Number]);
END;
GO

-- ---- Carry the prototype's rows across (there may be none — it never had a UI).
-- Old Stage: 0 NewLead, 1 Qualified, 2 SurveyBooked, 3 SurveyComplete, 4 AwaitingInformation,
--   5 DrawingsReceived, 6 FeasibilityReview, 7 Tendering, 8 ProposalIssued, 9 Negotiation,
--   10 Won, 11 Lost, 12 Nurture.
-- New Stage: 0 New, 1 Contacted, 2 Engaged, 3 SiteVisit, 4 Proposal, 5 Won, 6 Lost, 7 Nurture.
-- Old Source: 0 Website, 1 Instagram, 2 LinkedIn, 3 Referral, 4 Architect, 5 RepeatClient, 6 Manual.
-- New Source: 0 Strategy, 1 Inbound, 2 Referral, 3 Architect, 4 RepeatClient, 5 Manual.
-- Rows with Number = 0 are the un-migrated ones; the numbering at the end retires that marker,
-- so a second run of this batch touches nothing.
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906120000_AddSalesStrategies')
BEGIN
    EXEC sp_executesql N'
    UPDATE [Leads] SET [Stage] = CASE [Stage]
        WHEN 0 THEN 0
        WHEN 1 THEN 2
        WHEN 2 THEN 3
        WHEN 3 THEN 3
        WHEN 4 THEN 2
        WHEN 5 THEN 2
        WHEN 6 THEN 2
        WHEN 7 THEN 2
        WHEN 8 THEN 4
        WHEN 9 THEN 4
        WHEN 10 THEN 5
        WHEN 11 THEN 6
        WHEN 12 THEN 7
        ELSE 0 END
    WHERE [Number] = 0;

    UPDATE [Leads] SET [Source] = CASE [Source]
        WHEN 0 THEN 1
        WHEN 1 THEN 1
        WHEN 2 THEN 1
        WHEN 3 THEN 2
        WHEN 4 THEN 3
        WHEN 5 THEN 4
        WHEN 6 THEN 5
        ELSE 5 END
    WHERE [Number] = 0;

    UPDATE [Leads] SET [StageChangedAt] = [CapturedAt] WHERE [Number] = 0;

    UPDATE L SET L.[ProjectId] = O.[CreatedProjectId]
    FROM [Leads] L INNER JOIN [LeadOutcomes] O ON O.[LeadId] = L.[LeadId]
    WHERE L.[Number] = 0 AND O.[IsWon] = 1 AND O.[CreatedProjectId] IS NOT NULL;

    UPDATE L SET L.[LostReason] = O.[Reason]
    FROM [Leads] L INNER JOIN [LeadOutcomes] O ON O.[LeadId] = L.[LeadId]
    WHERE L.[Number] = 0 AND O.[IsWon] = 0;

    WITH Numbered AS (
        SELECT [LeadId], ROW_NUMBER() OVER (ORDER BY [CapturedAt], [LeadId]) AS N
        FROM [Leads] WHERE [Number] = 0)
    UPDATE L SET L.[Number] = Numbered.N
    FROM [Leads] L INNER JOIN Numbered ON Numbered.[LeadId] = L.[LeadId];
    ';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906120000_AddSalesStrategies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906120000_AddSalesStrategies', N'8.0.10');
END;
GO

COMMIT;
GO
