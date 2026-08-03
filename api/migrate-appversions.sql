-- Scoped apply script for 20260803180000_AddAppVersions (hand-written equivalent of
-- `dotnet ef migrations script <last-applied> --idempotent`, table + seed + history row).
-- Safe to run more than once: every step guards itself.

IF OBJECT_ID(N'[AppVersions]') IS NULL
BEGIN
    CREATE TABLE [AppVersions] (
        [AppVersionId] nvarchar(64) NOT NULL,
        [Version] bigint NOT NULL,
        [PublishedAt] datetimeoffset NOT NULL,
        [PublishedBy] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_AppVersions] PRIMARY KEY ([AppVersionId])
    );
END;
GO

-- Seed the single row at v1. Dynamic so this batch still compiles if the table is ever dropped
-- by a later migration (the lesson of SeparateArchitectsFromClients).
EXEC sp_executesql N'
IF NOT EXISTS (SELECT 1 FROM AppVersions WHERE AppVersionId = N''current'')
    INSERT INTO AppVersions (AppVersionId, Version, PublishedAt, PublishedBy)
    VALUES (N''current'', 1, SYSDATETIMEOFFSET(), N'''')';
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260803180000_AddAppVersions')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803180000_AddAppVersions', N'8.0.10');
END;
GO
