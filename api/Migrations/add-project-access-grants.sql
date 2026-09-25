-- ============================================================================
-- AddProjectAccessGrants  (2026-09-25)
-- ============================================================================
-- The projects an architect login may see, given to the login in Admin → Users:
-- ProjectAccessGrants, one row per login (Email) and project, unique on the pair.
-- A new, empty table. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260925090000_AddProjectAccessGrants.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the table.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-project-access-grants.sql -b -o add-project-access-grants.log
-- ============================================================================

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260925090000_AddProjectAccessGrants')
BEGIN
    IF OBJECT_ID(N'[ProjectAccessGrants]') IS NULL
    BEGIN
        CREATE TABLE [ProjectAccessGrants] (
            [ProjectAccessGrantId] nvarchar(64) NOT NULL,
            [Email] nvarchar(256) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [GrantedByEmail] nvarchar(256) NOT NULL,
            [GrantedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_ProjectAccessGrants] PRIMARY KEY ([ProjectAccessGrantId])
        );
        CREATE UNIQUE INDEX [IX_ProjectAccessGrants_Email_ProjectId] ON [ProjectAccessGrants] ([Email], [ProjectId]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260925090000_AddProjectAccessGrants')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260925090000_AddProjectAccessGrants', N'8.0.10');
END;
GO

COMMIT;
GO
