-- ============================================================================
-- AddProgressPhotoContentHashAndSiteNoteSenders  (2026-09-16)
-- ============================================================================
-- The FD's weekly-report spec, changes 1–3. Two additive columns:
--   ProgressPhotos.ContentHash      — SHA-256 of a photo as received; what "the same image
--                                     posted twice" is judged by. '' on photos stored before
--                                     this, which are therefore never deduplicated against.
--   Projects.SiteNoteSenderNames    — the people whose WhatsApp messages are the project's
--                                     site notes, one per line; NULL until someone lists them.
-- (ProgressUpdates.Description was already nvarchar(max); nothing to alter.)
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors
-- api/Migrations/20260916090000_AddProgressPhotoContentHashAndSiteNoteSenders.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-progress-photo-content-hash-and-site-note-senders.sql -b \
--          -o add-progress-photo-content-hash-and-site-note-senders.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916090000_AddProgressPhotoContentHashAndSiteNoteSenders')
BEGIN
    IF COL_LENGTH('ProgressPhotos', 'ContentHash') IS NULL
        ALTER TABLE [ProgressPhotos] ADD [ContentHash] nvarchar(64) NOT NULL
            CONSTRAINT [DF_ProgressPhotos_ContentHash] DEFAULT (N'');
    IF COL_LENGTH('Projects', 'SiteNoteSenderNames') IS NULL
        ALTER TABLE [Projects] ADD [SiteNoteSenderNames] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916090000_AddProgressPhotoContentHashAndSiteNoteSenders')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916090000_AddProgressPhotoContentHashAndSiteNoteSenders', N'8.0.10');
END;
GO

COMMIT;
GO
