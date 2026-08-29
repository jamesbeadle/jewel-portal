-- ============================================================================
-- AddRevertToOwnRole  (2026-08-28)
-- ============================================================================
-- One column: DirectoryUsers.RevertToOwnRole (bit, not null, default 0) — the
-- per-user opt-in, administered on Admin → Users, for the "Viewing as"
-- switcher defaulting back to the user's own role two hours after a switch
-- (built for the Finance Director, whose Administrator view kept sticking
-- across days). Default 0: everyone keeps today's sticky behaviour until the
-- toggle is ticked for them.
--
-- Additive only, so it is safe to apply BEFORE or WITH the deploy. Mirrors
-- api/Migrations/20260828150000_AddRevertToOwnRole.cs. Column guarded
-- individually (belt-and-braces on top of the history guard) and the
-- migration id is recorded in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-revert-to-own-role.sql -b -o add-revert-to-own-role.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828150000_AddRevertToOwnRole'
)
BEGIN
    IF COL_LENGTH('DirectoryUsers', 'RevertToOwnRole') IS NULL
        ALTER TABLE [DirectoryUsers] ADD [RevertToOwnRole] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828150000_AddRevertToOwnRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828150000_AddRevertToOwnRole', N'8.0.10');
END;
GO

COMMIT;
GO
