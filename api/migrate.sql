BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826180000_DropAgentTables'
)
BEGIN
    DROP TABLE [RequestAgents];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826180000_DropAgentTables'
)
BEGIN
    DROP TABLE [AgentChatMessages];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826180000_DropAgentTables'
)
BEGIN
    DROP TABLE [AgentProposals];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826180000_DropAgentTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260826180000_DropAgentTables', N'8.0.10');
END;
GO

COMMIT;
GO

