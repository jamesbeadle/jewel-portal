-- ============================================================================
-- AddSiteManual  (2026-09-29)
-- ============================================================================
-- The site manual as controlled modules: ManualModules (each module's working
-- text and controls beside the text the site sees), ManualModuleVersions (every
-- approved version, kept whole) and ManualAcknowledgements (who read which
-- version). Three new tables, nothing altered. Additive only.
--
-- House-style scoped script: applies the migration directly and records its id
-- in __EFMigrationsHistory so EF never re-applies it. Mirrors
-- api/Migrations/20260929120000_AddSiteManual.cs. Safe to apply BEFORE or WITH
-- the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-site-manual.sql -b -o add-site-manual.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260929120000_AddSiteManual')
BEGIN
    IF OBJECT_ID(N'[ManualModules]', N'U') IS NULL
    CREATE TABLE [ManualModules] (
        [ManualModuleId] nvarchar(64) NOT NULL,
        [Code] nvarchar(16) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Purpose] nvarchar(1000) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [PublishedBody] nvarchar(max) NOT NULL,
        [OwnerEmail] nvarchar(256) NOT NULL,
        [ApproverEmail] nvarchar(256) NOT NULL,
        [Status] int NOT NULL,
        [Version] int NOT NULL,
        [PublishedVersion] int NOT NULL,
        [ApprovedAt] datetimeoffset NULL,
        [ApprovedByEmail] nvarchar(256) NOT NULL,
        [NextReviewAt] datetimeoffset NULL,
        [IsForSiteManagers] bit NOT NULL,
        [IsForHealthAndSafetyOfficer] bit NOT NULL,
        [IsForForemen] bit NOT NULL,
        [LinkedFormSlugs] nvarchar(1000) NOT NULL,
        [LinkedStandards] nvarchar(1000) NOT NULL,
        [ChangeSummary] nvarchar(2000) NOT NULL,
        [SourceSections] nvarchar(256) NOT NULL,
        [Sequence] int NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedByEmail] nvarchar(256) NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ManualModules] PRIMARY KEY ([ManualModuleId])
    );

    IF OBJECT_ID(N'[ManualModuleVersions]', N'U') IS NULL
    CREATE TABLE [ManualModuleVersions] (
        [ManualModuleVersionId] nvarchar(64) NOT NULL,
        [ManualModuleId] nvarchar(64) NOT NULL,
        [Version] int NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [ChangeSummary] nvarchar(2000) NOT NULL,
        [ApprovedByEmail] nvarchar(256) NOT NULL,
        [ApprovedAt] datetimeoffset NOT NULL,
        [SupersededAt] datetimeoffset NULL,
        CONSTRAINT [PK_ManualModuleVersions] PRIMARY KEY ([ManualModuleVersionId])
    );

    IF OBJECT_ID(N'[ManualAcknowledgements]', N'U') IS NULL
    CREATE TABLE [ManualAcknowledgements] (
        [ManualAcknowledgementId] nvarchar(64) NOT NULL,
        [ManualModuleId] nvarchar(64) NOT NULL,
        [Version] int NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [TypedName] nvarchar(256) NOT NULL,
        [AcknowledgedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ManualAcknowledgements] PRIMARY KEY ([ManualAcknowledgementId])
    );

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ManualModules_Code')
        CREATE UNIQUE INDEX [IX_ManualModules_Code] ON [ManualModules] ([Code]);
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ManualModuleVersions_ManualModuleId_Version')
        CREATE UNIQUE INDEX [IX_ManualModuleVersions_ManualModuleId_Version] ON [ManualModuleVersions] ([ManualModuleId], [Version]);
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ManualAcknowledgements_ManualModuleId_Version_Email')
        CREATE UNIQUE INDEX [IX_ManualAcknowledgements_ManualModuleId_Version_Email] ON [ManualAcknowledgements] ([ManualModuleId], [Version], [Email]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929120000_AddSiteManual', N'8.0.10');
END;
GO

COMMIT;
GO
