-- ============================================================================
-- AddHsActionThread  (2026-09-23)
-- ============================================================================
-- The thread on a corrective action and the digest behind it (Katy-Louise's
-- asks of 15 Sep): HsRecordComments (what the site manager and the officer
-- say on an action), HsRecordPhotos (the photograph of the work done, stored
-- through the progress photo store), HsRecordEvents (the outbox the digest
-- sweep sends one email per project per sitting from), and the project's
-- site manager (Projects.SiteManagerName / SiteManagerEmail — a person, not a
-- login — whose address the digest goes to). Additive only; no FKs.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260923220000_AddHsActionThread.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the tables.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-hs-action-thread.sql -b -o add-hs-action-thread.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923220000_AddHsActionThread')
BEGIN
    IF COL_LENGTH(N'[Projects]', N'SiteManagerName') IS NULL
        ALTER TABLE [Projects] ADD [SiteManagerName] nvarchar(256) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'[Projects]', N'SiteManagerEmail') IS NULL
        ALTER TABLE [Projects] ADD [SiteManagerEmail] nvarchar(256) NOT NULL DEFAULT N'';

    IF OBJECT_ID(N'[HsRecordComments]', N'U') IS NULL
    BEGIN
        CREATE TABLE [HsRecordComments] (
            [HsRecordCommentId] nvarchar(64) NOT NULL,
            [HsRecordId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [AuthorEmail] nvarchar(256) NOT NULL,
            [AuthorName] nvarchar(256) NOT NULL,
            [Text] nvarchar(max) NOT NULL,
            [PostedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_HsRecordComments] PRIMARY KEY ([HsRecordCommentId])
        );
        CREATE INDEX [IX_HsRecordComments_HsRecordId] ON [HsRecordComments] ([HsRecordId]);
    END;

    IF OBJECT_ID(N'[HsRecordPhotos]', N'U') IS NULL
    BEGIN
        CREATE TABLE [HsRecordPhotos] (
            [HsRecordPhotoId] nvarchar(64) NOT NULL,
            [HsRecordId] nvarchar(64) NOT NULL,
            [HsRecordCommentId] nvarchar(64) NOT NULL,
            [FileName] nvarchar(512) NOT NULL,
            [BlobRef] nvarchar(1024) NOT NULL,
            [ContentType] nvarchar(256) NOT NULL,
            [FileSizeBytes] bigint NOT NULL,
            [ContentHash] nvarchar(64) NOT NULL,
            [UploadedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_HsRecordPhotos] PRIMARY KEY ([HsRecordPhotoId])
        );
        CREATE INDEX [IX_HsRecordPhotos_HsRecordId] ON [HsRecordPhotos] ([HsRecordId]);
    END;

    IF OBJECT_ID(N'[HsRecordEvents]', N'U') IS NULL
    BEGIN
        CREATE TABLE [HsRecordEvents] (
            [HsRecordEventId] nvarchar(64) NOT NULL,
            [HsRecordId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [Kind] int NOT NULL,
            [Detail] nvarchar(1024) NOT NULL,
            [ByEmail] nvarchar(256) NOT NULL,
            [ByName] nvarchar(256) NOT NULL,
            [OccurredAt] datetimeoffset NOT NULL,
            [NotifiedAt] datetimeoffset NULL,
            CONSTRAINT [PK_HsRecordEvents] PRIMARY KEY ([HsRecordEventId])
        );
        CREATE INDEX [IX_HsRecordEvents_NotifiedAt] ON [HsRecordEvents] ([NotifiedAt]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923220000_AddHsActionThread')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923220000_AddHsActionThread', N'8.0.10');
END;
GO

COMMIT;
GO
