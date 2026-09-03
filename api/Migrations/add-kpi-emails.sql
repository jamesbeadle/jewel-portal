-- ============================================================================
-- AddKpiEmails  (2026-09-03)
-- ============================================================================
-- Two new tables: KpiPeople — the people KPIs are filed under (a portal user
-- by directory email, or someone added by name alone) — and KpiEmails, an
-- email marked as a KPI against one of them (administrators only). Nothing
-- is tagged in the mailbox: the row is the mark.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260903120000_AddKpiEmails.cs.
--
-- Additive only, so it is safe to apply BEFORE or WITH the deploy. Tables
-- guarded individually (belt-and-braces on top of the history guard).
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-kpi-emails.sql -b -o add-kpi-emails.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903120000_AddKpiEmails'
)
BEGIN
    IF OBJECT_ID('KpiPeople', 'U') IS NULL
    BEGIN
        CREATE TABLE [KpiPeople] (
            [KpiPersonId] nvarchar(64) NOT NULL,
            [Name] nvarchar(256) NOT NULL,
            [Email] nvarchar(256) NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_KpiPeople] PRIMARY KEY ([KpiPersonId])
        );
        CREATE INDEX [IX_KpiPeople_Email] ON [KpiPeople] ([Email]);
    END;

    IF OBJECT_ID('KpiEmails', 'U') IS NULL
    BEGIN
        CREATE TABLE [KpiEmails] (
            [KpiEmailId] nvarchar(64) NOT NULL,
            [PersonId] nvarchar(64) NOT NULL,
            [MessageId] nvarchar(512) NOT NULL,
            [InternetMessageId] nvarchar(512) NULL,
            [ConversationId] nvarchar(512) NULL,
            [Subject] nvarchar(1024) NOT NULL,
            [FromEmail] nvarchar(256) NOT NULL,
            [FromName] nvarchar(256) NOT NULL,
            [ReceivedAt] datetimeoffset NOT NULL,
            [Note] nvarchar(2048) NOT NULL,
            [MarkedByEmail] nvarchar(256) NOT NULL,
            [MarkedAt] datetimeoffset NOT NULL,
            [Number] int NOT NULL,
            CONSTRAINT [PK_KpiEmails] PRIMARY KEY ([KpiEmailId])
        );
        CREATE INDEX [IX_KpiEmails_PersonId] ON [KpiEmails] ([PersonId]);
        CREATE INDEX [IX_KpiEmails_Number] ON [KpiEmails] ([Number]);
        CREATE INDEX [IX_KpiEmails_InternetMessageId] ON [KpiEmails] ([InternetMessageId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903120000_AddKpiEmails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903120000_AddKpiEmails', N'8.0.10');
END;
GO

COMMIT;
GO
