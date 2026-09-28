-- ============================================================================
-- AddSiteDrawingLinks  (2026-09-28)
-- ============================================================================
-- The site drawing links: a QR poster's link onto one folder of a project's
-- document register - the folder, its label, expiry, revocation and scan count,
-- and only the SHA-256 of the secret. Additive only: one table, two indexes.
--
-- House-style scoped script, as add-site-photo-archive.sql: applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260928115521_AddSiteDrawingLinks.cs.
-- Apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-site-drawing-links.sql -b -o add-site-drawing-links.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928115521_AddSiteDrawingLinks'
)
BEGIN
    CREATE TABLE [SiteDrawingLinks] (
        [SiteDrawingLinkId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [DrawingFolderId] nvarchar(64) NOT NULL,
        [IncludeSubFolders] bit NOT NULL,
        [Label] nvarchar(128) NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [ScanCount] int NOT NULL,
        [LastScannedAt] datetimeoffset NULL,
        CONSTRAINT [PK_SiteDrawingLinks] PRIMARY KEY ([SiteDrawingLinkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928115521_AddSiteDrawingLinks'
)
BEGIN
    CREATE INDEX [IX_SiteDrawingLinks_ProjectId] ON [SiteDrawingLinks] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928115521_AddSiteDrawingLinks'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SiteDrawingLinks_TokenHash] ON [SiteDrawingLinks] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928115521_AddSiteDrawingLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928115521_AddSiteDrawingLinks', N'8.0.10');
END;
GO

COMMIT;
GO

