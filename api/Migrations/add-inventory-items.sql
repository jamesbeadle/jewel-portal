-- ============================================================================
-- AddInventoryItems  (2026-08-29)
-- ============================================================================
-- One new table: InventoryItems — the project inventory register (a product
-- held for the job and where it's kept; INV-#### numbers are the mailbox tag
-- stems for the Control Centre's Supplier pathway).
--
-- Why this scoped script exists: the migration id 20260828130000 sorts BEFORE
-- the already-applied 20260828150000_AddRevertToOwnRole, so the standard
-- "script from the last applied id" flow skips it forever. This script applies
-- it directly, and records the id in __EFMigrationsHistory so EF never
-- re-applies it (a fresh-database rebuild still runs it in id order fine).
--
-- Additive only, so it is safe to apply BEFORE or WITH the deploy. Mirrors
-- api/Migrations/20260828130000_AddInventoryItems.cs. Table guarded
-- individually (belt-and-braces on top of the history guard).
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-inventory-items.sql -b -o add-inventory-items.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828130000_AddInventoryItems'
)
BEGIN
    IF OBJECT_ID('InventoryItems', 'U') IS NULL
    BEGIN
        CREATE TABLE [InventoryItems] (
            [InventoryItemId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [ProductName] nvarchar(256) NOT NULL,
            [ProductDetails] nvarchar(2048) NOT NULL,
            [Location] nvarchar(256) NOT NULL,
            [LocationDetails] nvarchar(2048) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [Number] int NOT NULL,
            CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([InventoryItemId])
        );

        CREATE INDEX [IX_InventoryItems_ProjectId] ON [InventoryItems] ([ProjectId]);
        CREATE INDEX [IX_InventoryItems_Number] ON [InventoryItems] ([Number]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828130000_AddInventoryItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828130000_AddInventoryItems', N'8.0.10');
END;
GO

COMMIT;
GO
