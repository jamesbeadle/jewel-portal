-- ============================================================================
-- AddImagine  (2026-09-06)
-- ============================================================================
-- The post-identification journey: the lead's private imagine link
-- (Leads.ImagineToken — the QR code), the ImagineRounds / ImagineImages
-- tables behind the public /imagine/{token} page and its AI renders, and
-- SalesProposals — the scoping/pricing stage with its acceptance record.
-- Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260906200000_AddImagine.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the new columns. Every object guarded on its own.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-imagine.sql -b -o add-imagine.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906200000_AddImagine')
BEGIN
    IF COL_LENGTH('Leads', 'ImagineToken') IS NULL
        ALTER TABLE [Leads] ADD [ImagineToken] nvarchar(64) NULL;
    IF COL_LENGTH('Leads', 'ImagineTokenIssuedAt') IS NULL
        ALTER TABLE [Leads] ADD [ImagineTokenIssuedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906200000_AddImagine')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Leads_ImagineToken' AND object_id = OBJECT_ID('Leads'))
        CREATE UNIQUE INDEX [IX_Leads_ImagineToken] ON [Leads] ([ImagineToken]) WHERE [ImagineToken] IS NOT NULL;

    IF OBJECT_ID(N'[ImagineRounds]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ImagineRounds] (
            [RoundId] nvarchar(64) NOT NULL,
            [LeadId] nvarchar(64) NOT NULL,
            [Number] int NOT NULL,
            [Kind] int NOT NULL,
            [Brief] nvarchar(4000) NOT NULL,
            [BasedOnImageId] nvarchar(64) NULL,
            [Status] int NOT NULL,
            [Error] nvarchar(2000) NULL,
            [RequestedAt] datetimeoffset NOT NULL,
            [StartedAt] datetimeoffset NULL,
            [CompletedAt] datetimeoffset NULL,
            [Observations] nvarchar(max) NOT NULL,
            [ProspectName] nvarchar(256) NOT NULL,
            [ProspectEmail] nvarchar(256) NOT NULL,
            [ClientHash] nvarchar(64) NULL,
            CONSTRAINT [PK_ImagineRounds] PRIMARY KEY ([RoundId])
        );
        CREATE INDEX [IX_ImagineRounds_LeadId] ON [ImagineRounds] ([LeadId]);
        CREATE INDEX [IX_ImagineRounds_RequestedAt] ON [ImagineRounds] ([RequestedAt]);
    END;

    IF OBJECT_ID(N'[ImagineImages]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ImagineImages] (
            [ImageId] nvarchar(64) NOT NULL,
            [LeadId] nvarchar(64) NOT NULL,
            [RoundId] nvarchar(64) NOT NULL,
            [Kind] int NOT NULL,
            [Order] int NOT NULL,
            [Title] nvarchar(256) NOT NULL,
            [Description] nvarchar(2000) NOT NULL,
            [Prompt] nvarchar(max) NOT NULL,
            [BlobRef] nvarchar(512) NOT NULL,
            [ContentType] nvarchar(128) NOT NULL,
            [Size] bigint NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [Liked] bit NOT NULL,
            [Comment] nvarchar(2000) NOT NULL,
            CONSTRAINT [PK_ImagineImages] PRIMARY KEY ([ImageId])
        );
        CREATE INDEX [IX_ImagineImages_LeadId] ON [ImagineImages] ([LeadId]);
        CREATE INDEX [IX_ImagineImages_RoundId] ON [ImagineImages] ([RoundId]);
    END;

    IF OBJECT_ID(N'[SalesProposals]', N'U') IS NULL
    BEGIN
        CREATE TABLE [SalesProposals] (
            [ProposalId] nvarchar(64) NOT NULL,
            [LeadId] nvarchar(64) NOT NULL,
            [Version] int NOT NULL,
            [Title] nvarchar(256) NOT NULL,
            [Scope] nvarchar(max) NOT NULL,
            [BasePrice] decimal(18,4) NOT NULL,
            [OptionsJson] nvarchar(max) NOT NULL,
            [ScheduleJson] nvarchar(max) NOT NULL,
            [Terms] nvarchar(max) NOT NULL,
            [HeroImageId] nvarchar(64) NULL,
            [Status] int NOT NULL,
            [CreatedByEmail] nvarchar(256) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [UpdatedAt] datetimeoffset NOT NULL,
            [SentAt] datetimeoffset NULL,
            [AcceptedAt] datetimeoffset NULL,
            [AcceptedByName] nvarchar(256) NULL,
            [AcceptedByEmail] nvarchar(256) NULL,
            [AcceptedOptionIdsJson] nvarchar(max) NOT NULL,
            [AcceptedPrice] decimal(18,4) NULL,
            [AcceptedClientHash] nvarchar(64) NULL,
            [DeclinedAt] datetimeoffset NULL,
            [DeclineReason] nvarchar(1024) NULL,
            CONSTRAINT [PK_SalesProposals] PRIMARY KEY ([ProposalId])
        );
        CREATE INDEX [IX_SalesProposals_LeadId] ON [SalesProposals] ([LeadId]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260906200000_AddImagine')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906200000_AddImagine', N'8.0.10');
END;
GO

COMMIT;
GO
