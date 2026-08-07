BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807090000_AddRequestCriticalPathNudgeDismissed'
)
BEGIN
    ALTER TABLE [Requests] ADD [CriticalPathNudgeDismissed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807090000_AddRequestCriticalPathNudgeDismissed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807090000_AddRequestCriticalPathNudgeDismissed', N'8.0.10');
END;
GO

COMMIT;
GO

