BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826100000_AddValuationLineItemClientReference'
)
BEGIN
    ALTER TABLE [ValuationLineItems] ADD [ClientReference] nvarchar(64) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826100000_AddValuationLineItemClientReference'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260826100000_AddValuationLineItemClientReference', N'8.0.10');
END;
GO

COMMIT;
GO

