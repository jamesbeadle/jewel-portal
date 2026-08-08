-- ============================================================================
-- DropRequestRaisedTo  (2026-08-08)
-- ============================================================================
-- Removes the "Raised to" concept from Requests: the free-text display column
-- and the structured project-contact link behind the retired dropdown.
-- Mirrors api/Migrations/20260808120000_DropRequestRaisedTo.cs and records
-- itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808120000_DropRequestRaisedTo'
)
BEGIN
    ALTER TABLE [Requests] DROP COLUMN [RaisedTo];
    ALTER TABLE [Requests] DROP COLUMN [RaisedToContactId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808120000_DropRequestRaisedTo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260808120000_DropRequestRaisedTo', N'8.0.10');
END;
GO

COMMIT;
GO
