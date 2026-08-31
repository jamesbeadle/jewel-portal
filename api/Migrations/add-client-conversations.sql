-- ============================================================================
-- AddClientConversations  (2026-08-31)
-- ============================================================================
-- Client conversations on RFIs and Variation Orders:
--   - RequestMessages.ParentMessageId (nvarchar(64) NULL) — threads the request
--     conversation; null = top-level (all existing rows, and every email leg).
--   - DirectoryUsers.ClientId (nvarchar(64) NULL) — links a login to a client
--     account, the way SubcontractorId links a portal subcontractor.
--   - VariationOrderMessages — a variation order's own in-app conversation
--     (requests had RequestMessages; variations only had live tagged email).
--
-- Why this scoped script exists: the migration id 20260831160000 sorts BEFORE
-- the same-day 20260831220000_AddWorkerSettlementIdentityAndChaseDismissals,
-- so once that is applied the standard "script from the last applied id" flow
-- would skip this one forever. This script applies it directly and records the
-- id in __EFMigrationsHistory so EF never re-applies it (a fresh-database
-- rebuild still runs it in id order fine).
--
-- Additive only, so it is safe to apply BEFORE or WITH the deploy. Mirrors
-- api/Migrations/20260831160000_AddClientConversations.cs. Each piece guarded
-- individually (belt-and-braces on top of the history guard).
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-client-conversations.sql -b -o add-client-conversations.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831160000_AddClientConversations'
)
BEGIN
    IF COL_LENGTH('RequestMessages', 'ParentMessageId') IS NULL
    BEGIN
        ALTER TABLE [RequestMessages] ADD [ParentMessageId] nvarchar(64) NULL;
    END;

    IF COL_LENGTH('DirectoryUsers', 'ClientId') IS NULL
    BEGIN
        ALTER TABLE [DirectoryUsers] ADD [ClientId] nvarchar(64) NULL;
    END;

    IF OBJECT_ID('VariationOrderMessages', 'U') IS NULL
    BEGIN
        CREATE TABLE [VariationOrderMessages] (
            [MessageId] nvarchar(64) NOT NULL,
            [VariationOrderId] nvarchar(64) NOT NULL,
            [AuthorEmail] nvarchar(256) NOT NULL,
            [AuthorName] nvarchar(256) NOT NULL,
            [Body] nvarchar(4000) NOT NULL,
            [Visibility] int NOT NULL,
            [PostedAt] datetimeoffset NOT NULL,
            [ParentMessageId] nvarchar(64) NULL,
            CONSTRAINT [PK_VariationOrderMessages] PRIMARY KEY ([MessageId])
        );
        CREATE INDEX [IX_VariationOrderMessages_VariationOrderId]
            ON [VariationOrderMessages] ([VariationOrderId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831160000_AddClientConversations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831160000_AddClientConversations', N'8.0.10');
END;
GO

COMMIT;
GO
