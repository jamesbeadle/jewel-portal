-- ============================================================================
-- AddSiteInstructions  (2026-09-03)
-- ============================================================================
-- One new table: SiteInstructions — the project site-instruction register (a
-- written instruction to site under a short title, with where it applies;
-- SI-#### numbers are the mailbox tag stems for the Control Centre's Internal
-- pathway).
--
-- House-style scoped script: additive only, so it is safe to apply BEFORE or
-- WITH the deploy. Mirrors api/Migrations/20260903150000_AddSiteInstructions.cs
-- and records the id in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-site-instructions.sql -b -o add-site-instructions.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903150000_AddSiteInstructions'
)
BEGIN
    IF OBJECT_ID('SiteInstructions', 'U') IS NULL
    BEGIN
        CREATE TABLE [SiteInstructions] (
            [SiteInstructionId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [Title] nvarchar(256) NOT NULL,
            [Instruction] nvarchar(4000) NOT NULL,
            [Location] nvarchar(256) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [Number] int NOT NULL,
            CONSTRAINT [PK_SiteInstructions] PRIMARY KEY ([SiteInstructionId])
        );

        CREATE INDEX [IX_SiteInstructions_ProjectId] ON [SiteInstructions] ([ProjectId]);
        CREATE INDEX [IX_SiteInstructions_Number] ON [SiteInstructions] ([Number]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903150000_AddSiteInstructions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903150000_AddSiteInstructions', N'8.0.10');
END;
GO

COMMIT;
GO
