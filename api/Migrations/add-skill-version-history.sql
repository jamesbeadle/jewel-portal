-- ============================================================================
-- AddSkillVersionHistory  (2026-09-24)
-- ============================================================================
-- Every past version of a stored skill and its reference documents can be
-- audited and restored. SkillRevisions gains DisplayName, IsPinned, IsActive and
-- WrittenAt (NULL on revisions kept before today — the page reads the written
-- time off the previous version's replacement instead); SkillReferences gains
-- Version (1 for every existing reference); SkillReferenceRevisions is new and
-- empty. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260924140000_AddSkillVersionHistory.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-skill-version-history.sql -b -o add-skill-version-history.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924140000_AddSkillVersionHistory')
BEGIN
    IF COL_LENGTH(N'[SkillRevisions]', N'DisplayName') IS NULL
        ALTER TABLE [SkillRevisions] ADD [DisplayName] nvarchar(256) NOT NULL
            CONSTRAINT [DF_SkillRevisions_DisplayName] DEFAULT N'';
    IF COL_LENGTH(N'[SkillRevisions]', N'IsPinned') IS NULL
        ALTER TABLE [SkillRevisions] ADD [IsPinned] bit NULL;
    IF COL_LENGTH(N'[SkillRevisions]', N'IsActive') IS NULL
        ALTER TABLE [SkillRevisions] ADD [IsActive] bit NULL;
    IF COL_LENGTH(N'[SkillRevisions]', N'WrittenAt') IS NULL
        ALTER TABLE [SkillRevisions] ADD [WrittenAt] datetimeoffset NULL;
    IF COL_LENGTH(N'[SkillReferences]', N'Version') IS NULL
        ALTER TABLE [SkillReferences] ADD [Version] int NOT NULL
            CONSTRAINT [DF_SkillReferences_Version] DEFAULT 1;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924140000_AddSkillVersionHistory')
   AND OBJECT_ID(N'[SkillReferenceRevisions]') IS NULL
BEGIN
    CREATE TABLE [SkillReferenceRevisions] (
        [SkillReferenceRevisionId] nvarchar(64) NOT NULL,
        [SkillKey] nvarchar(128) NOT NULL,
        [RefKey] nvarchar(128) NOT NULL,
        [Version] int NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [WrittenByEmail] nvarchar(256) NOT NULL,
        [WrittenAt] datetimeoffset NOT NULL,
        [ReplacedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_SkillReferenceRevisions] PRIMARY KEY ([SkillReferenceRevisionId])
    );
    CREATE INDEX [IX_SkillReferenceRevisions_SkillKey_RefKey_Version]
        ON [SkillReferenceRevisions] ([SkillKey], [RefKey], [Version]);
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924140000_AddSkillVersionHistory')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924140000_AddSkillVersionHistory', N'8.0.10');
END;
GO

COMMIT;
GO
