BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902100000_WidenClaimPercentPrecision'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClaimLines]') AND [c].[name] = N'PercentComplete');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ClaimLines] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ClaimLines] ALTER COLUMN [PercentComplete] decimal(28,20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902100000_WidenClaimPercentPrecision'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ValuationReportSnapshotLines]') AND [c].[name] = N'PercentComplete');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ValuationReportSnapshotLines] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [ValuationReportSnapshotLines] ALTER COLUMN [PercentComplete] decimal(28,20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902100000_WidenClaimPercentPrecision'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902100000_WidenClaimPercentPrecision', N'8.0.10');
END;
GO

COMMIT;
GO

