-- ============================================================================
-- AddDirectoryUserArchitectId  (2026-09-21)
-- ============================================================================
-- The architect's identity on a login: DirectoryUsers.ArchitectId, the
-- architect twin of ClientId and SubcontractorId. Set by the architect portal
-- invite (POST architects/{id}/portal-invite); read by ArchitectScope so an
-- architect login reaches only the projects that name their practice as the
-- party. NULL for every existing row — an architect invited before this lands
-- reaches nothing until re-invited or linked by hand. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260921090000_AddDirectoryUserArchitectId.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the column.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-directory-user-architect-id.sql -b -o add-directory-user-architect-id.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260921090000_AddDirectoryUserArchitectId')
BEGIN
    IF COL_LENGTH(N'[DirectoryUsers]', N'ArchitectId') IS NULL
        ALTER TABLE [DirectoryUsers] ADD [ArchitectId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260921090000_AddDirectoryUserArchitectId')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260921090000_AddDirectoryUserArchitectId', N'8.0.10');
END;
GO

COMMIT;
GO
