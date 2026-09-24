-- ============================================================================
-- AddSitePhotoArchive  (2026-09-24)
-- ============================================================================
-- The site photo pool's archive: a photograph the weekly-report run judges not
-- to be progress (a screenshot, a drawing, a marked-up photo, a snag) is set
-- aside with its reason and the project's week it was dumped in, and kept.
-- Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260924100000_AddSitePhotoArchive.cs.
-- Apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-site-photo-archive.sql -b -o add-site-photo-archive.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924100000_AddSitePhotoArchive')
BEGIN
    IF COL_LENGTH(N'SitePhotos', N'ArchivedAt') IS NULL
        ALTER TABLE [SitePhotos] ADD [ArchivedAt] datetimeoffset NULL;
    IF COL_LENGTH(N'SitePhotos', N'ArchiveReason') IS NULL
        ALTER TABLE [SitePhotos] ADD [ArchiveReason] int NULL;
    IF COL_LENGTH(N'SitePhotos', N'ArchiveNote') IS NULL
        ALTER TABLE [SitePhotos] ADD [ArchiveNote] nvarchar(1024) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'SitePhotos', N'ArchivedForProjectId') IS NULL
        ALTER TABLE [SitePhotos] ADD [ArchivedForProjectId] nvarchar(64) NULL;
    IF COL_LENGTH(N'SitePhotos', N'ArchivedForPeriodEnd') IS NULL
        ALTER TABLE [SitePhotos] ADD [ArchivedForPeriodEnd] date NULL;
    IF COL_LENGTH(N'SitePhotos', N'ArchivedByEmail') IS NULL
        ALTER TABLE [SitePhotos] ADD [ArchivedByEmail] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924100000_AddSitePhotoArchive')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924100000_AddSitePhotoArchive', N'8.0.10');
END;
GO

COMMIT;
GO
