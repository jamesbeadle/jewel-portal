-- ============================================================================
-- AddSitePhotos  (2026-09-16)
-- ============================================================================
-- The company-wide site photo pool ("a big dumping ground for photos and any
-- project"): photographs dropped in before anyone has said which project or
-- day they belong to. Keyed by the SHA-256 of the file (unique) so the
-- assistant finds a row from a laptop-side fingerprint of the same file and
-- files it onto the right progress update; the FiledTo* columns record where
-- it went. Additive only; no FKs, as everywhere else.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260916230000_AddSitePhotos.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the table.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-site-photos.sql -b -o add-site-photos.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916230000_AddSitePhotos')
BEGIN
    IF OBJECT_ID(N'[SitePhotos]', N'U') IS NULL
    BEGIN
        CREATE TABLE [SitePhotos] (
            [SitePhotoId] nvarchar(64) NOT NULL,
            [FileName] nvarchar(512) NOT NULL,
            [BlobRef] nvarchar(1024) NOT NULL,
            [ContentType] nvarchar(256) NOT NULL,
            [FileSizeBytes] bigint NOT NULL,
            [ContentHash] nvarchar(64) NOT NULL,
            [UploadedByEmail] nvarchar(256) NOT NULL,
            [UploadedAt] datetimeoffset NOT NULL,
            [FiledToProjectId] nvarchar(64) NULL,
            [FiledToProgressUpdateId] nvarchar(64) NULL,
            [FiledToProgressPhotoId] nvarchar(64) NULL,
            [FiledByEmail] nvarchar(256) NOT NULL,
            [FiledAt] datetimeoffset NULL,
            CONSTRAINT [PK_SitePhotos] PRIMARY KEY ([SitePhotoId])
        );
        CREATE UNIQUE INDEX [IX_SitePhotos_ContentHash] ON [SitePhotos] ([ContentHash]);
        CREATE INDEX [IX_SitePhotos_FiledToProgressUpdateId] ON [SitePhotos] ([FiledToProgressUpdateId]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916230000_AddSitePhotos')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916230000_AddSitePhotos', N'8.0.10');
END;
GO

COMMIT;
GO
