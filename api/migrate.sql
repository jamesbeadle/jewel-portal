BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827200000_DropAiChatTables'
)
BEGIN
    DROP TABLE [AiPendingReplies];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827200000_DropAiChatTables'
)
BEGIN
    DROP TABLE [AiAttachments];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827200000_DropAiChatTables'
)
BEGIN
    DROP TABLE [AiConversationMessages];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827200000_DropAiChatTables'
)
BEGIN
    DROP TABLE [AiConversations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827200000_DropAiChatTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827200000_DropAiChatTables', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828100000_AddOAuthClientSecret'
)
BEGIN
    ALTER TABLE [OAuthClients] ADD [SecretHash] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828100000_AddOAuthClientSecret'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828100000_AddOAuthClientSecret', N'8.0.10');
END;
GO

COMMIT;
GO

