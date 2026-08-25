-- ============================================================================
-- AddDrawingSubFolders  (2026-08-25)
-- ============================================================================
-- Drawing folders nest: one nullable column, DrawingFolders.ParentDrawingFolderId
-- (null = top level), plus its index. Additive only, so apply it BEFORE the
-- deploy: the deployed API maps the column and every folder read would 500
-- until it exists.
--
-- Mirrors api/Migrations/20260825150000_AddDrawingSubFolders.cs. Each object is
-- guarded individually on top of the history guard, and the migration id is
-- recorded in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i api/Migrations/add-drawing-sub-folders.sql -b -o migrate.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825150000_AddDrawingSubFolders'
)
BEGIN
    IF COL_LENGTH('DrawingFolders', 'ParentDrawingFolderId') IS NULL
        ALTER TABLE [DrawingFolders] ADD [ParentDrawingFolderId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = N'IX_DrawingFolders_ParentDrawingFolderId' AND object_id = OBJECT_ID(N'[DrawingFolders]')
)
    CREATE INDEX [IX_DrawingFolders_ParentDrawingFolderId] ON [DrawingFolders] ([ParentDrawingFolderId]);
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825150000_AddDrawingSubFolders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825150000_AddDrawingSubFolders', N'8.0.10');
END;
GO

COMMIT;
GO
