BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730100000_AddDirectoryUserRevocation'
)
BEGIN
    ALTER TABLE [DirectoryUsers] ADD [RevokedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730100000_AddDirectoryUserRevocation'
)
BEGIN
    ALTER TABLE [DirectoryUsers] ADD [RevokedBy] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730100000_AddDirectoryUserRevocation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730100000_AddDirectoryUserRevocation', N'8.0.10');
END;
GO

COMMIT;
GO

