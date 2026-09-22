-- ============================================================================
-- AddUsefulInformationSecret  (2026-09-22)
-- ============================================================================
-- The shared site credential a Useful Information note may hold:
-- UsefulInformationNotes.SecretCiphertext, AES-GCM encrypted by the api under
-- the app setting UsefulInformation__SecretKey (32 random bytes, base64) and
-- revealed to the directors alone, every reveal on the audit trail. NULL for
-- every existing note. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260922150000_AddUsefulInformationSecret.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the column.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-useful-information-secret.sql -b -o add-useful-information-secret.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260922150000_AddUsefulInformationSecret')
BEGIN
    IF COL_LENGTH(N'[UsefulInformationNotes]', N'SecretCiphertext') IS NULL
        ALTER TABLE [UsefulInformationNotes] ADD [SecretCiphertext] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260922150000_AddUsefulInformationSecret')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260922150000_AddUsefulInformationSecret', N'8.0.10');
END;
GO

COMMIT;
GO
