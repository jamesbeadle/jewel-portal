-- ============================================================================
-- AddTodoActivity  (2026-08-25)
-- ============================================================================
-- The to-do timeline: a TodoItemActivities table (one row per change, logged
-- chase, or email sent from the item's page) and two nullable columns on
-- TodoItems (StartedAt / StartedByEmail — the In-progress stamp; null = Open).
-- Additive only, so apply it BEFORE the deploy: the deployed API maps the new
-- columns and every to-do read would 500 until they exist.
--
-- Mirrors api/Migrations/20260825120000_AddTodoActivity.cs. Each object is
-- guarded individually on top of the history guard, and the migration id is
-- recorded in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i api/Migrations/add-todo-activity.sql -b -o migrate.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825120000_AddTodoActivity'
)
BEGIN
    IF COL_LENGTH('TodoItems', 'StartedAt') IS NULL
        ALTER TABLE [TodoItems] ADD [StartedAt] datetimeoffset NULL;
    IF COL_LENGTH('TodoItems', 'StartedByEmail') IS NULL
        ALTER TABLE [TodoItems] ADD [StartedByEmail] nvarchar(256) NULL;

    IF OBJECT_ID(N'[TodoItemActivities]', N'U') IS NULL
    BEGIN
        CREATE TABLE [TodoItemActivities] (
            [TodoItemActivityId] nvarchar(64) NOT NULL,
            [TodoItemId] nvarchar(64) NOT NULL,
            [Kind] int NOT NULL,
            [Summary] nvarchar(512) NOT NULL,
            [ActorEmail] nvarchar(256) NOT NULL,
            [OccurredAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_TodoItemActivities] PRIMARY KEY ([TodoItemActivityId])
        );
        CREATE INDEX [IX_TodoItemActivities_TodoItemId] ON [TodoItemActivities] ([TodoItemId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825120000_AddTodoActivity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825120000_AddTodoActivity', N'8.0.10');
END;
GO

COMMIT;
GO
