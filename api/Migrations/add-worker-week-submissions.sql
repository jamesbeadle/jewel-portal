-- ============================================================================
-- AddWorkerWeekSubmissions  (2026-10-01)
-- ============================================================================
-- The weeks operatives submit from My day for the office to sign off in one
-- step: one row per worker and week — when it was submitted, where it has got
-- to (0 submitted, 1 signed off, 2 sent back), who answered and their note.
-- Additive only.
--
-- House-style scoped script: applies the migration directly and records its id
-- in __EFMigrationsHistory so EF never re-applies it. Mirrors
-- api/Migrations/20261001090000_AddWorkerWeekSubmissions.cs. Safe to apply
-- BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-worker-week-submissions.sql -b -o add-worker-week-submissions.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261001090000_AddWorkerWeekSubmissions')
BEGIN
    IF OBJECT_ID(N'[WorkerWeekSubmissions]', N'U') IS NULL
    BEGIN
        CREATE TABLE [WorkerWeekSubmissions] (
            [WorkerWeekSubmissionId] nvarchar(64) NOT NULL,
            [WorkerId] nvarchar(64) NOT NULL,
            [WeekStart] datetimeoffset NOT NULL,
            [Status] int NOT NULL,
            [SubmittedAt] datetimeoffset NOT NULL,
            [ReviewedByEmail] nvarchar(256) NOT NULL,
            [ReviewedAt] datetimeoffset NULL,
            [ReviewNote] nvarchar(max) NOT NULL,
            CONSTRAINT [PK_WorkerWeekSubmissions] PRIMARY KEY ([WorkerWeekSubmissionId])
        );
        CREATE UNIQUE INDEX [IX_WorkerWeekSubmissions_WorkerId_WeekStart]
            ON [WorkerWeekSubmissions] ([WorkerId], [WeekStart]);
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001090000_AddWorkerWeekSubmissions', N'8.0.10');
END;
GO

COMMIT;
GO
