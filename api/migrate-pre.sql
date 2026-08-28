BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827160000_AddWeeklyCashflowPlanning'
)
BEGIN
    CREATE TABLE [WeeklyCashflowItems] (
        [WeeklyCashflowItemId] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Category] int NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Recurrence] int NOT NULL,
        [FirstDueOn] datetimeoffset NOT NULL,
        [LastDueOn] datetimeoffset NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ArchivedAt] datetimeoffset NULL,
        [ArchivedByEmail] nvarchar(256) NULL,
        CONSTRAINT [PK_WeeklyCashflowItems] PRIMARY KEY ([WeeklyCashflowItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827160000_AddWeeklyCashflowPlanning'
)
BEGIN
    CREATE TABLE [WeeklyCashflowPlacements] (
        [PlacementKey] nvarchar(128) NOT NULL,
        [PlannedWeekStart] datetimeoffset NOT NULL,
        [MovedByEmail] nvarchar(256) NOT NULL,
        [MovedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WeeklyCashflowPlacements] PRIMARY KEY ([PlacementKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827160000_AddWeeklyCashflowPlanning'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827160000_AddWeeklyCashflowPlanning', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    CREATE TABLE [OAuthClients] (
        [ClientId] nvarchar(64) NOT NULL,
        [ClientName] nvarchar(128) NOT NULL,
        [RedirectUrisJson] nvarchar(4000) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_OAuthClients] PRIMARY KEY ([ClientId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    CREATE TABLE [OAuthAuthCodes] (
        [CodeHash] nvarchar(128) NOT NULL,
        [ClientId] nvarchar(64) NOT NULL,
        [UserEmail] nvarchar(256) NOT NULL,
        [RedirectUri] nvarchar(1024) NOT NULL,
        [CodeChallenge] nvarchar(128) NOT NULL,
        [Scope] nvarchar(256) NOT NULL,
        [Resource] nvarchar(512) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [UsedAt] datetimeoffset NULL,
        CONSTRAINT [PK_OAuthAuthCodes] PRIMARY KEY ([CodeHash])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    CREATE TABLE [OAuthTokens] (
        [TokenHash] nvarchar(128) NOT NULL,
        [Kind] int NOT NULL,
        [UserEmail] nvarchar(256) NOT NULL,
        [ClientId] nvarchar(64) NOT NULL,
        [ClientName] nvarchar(128) NOT NULL,
        [Scope] nvarchar(256) NOT NULL,
        [FamilyId] nvarchar(128) NULL,
        [IssuedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [LastUsedAt] datetimeoffset NULL,
        CONSTRAINT [PK_OAuthTokens] PRIMARY KEY ([TokenHash])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    CREATE INDEX [IX_OAuthTokens_UserEmail_Kind] ON [OAuthTokens] ([UserEmail], [Kind]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    CREATE INDEX [IX_OAuthTokens_FamilyId] ON [OAuthTokens] ([FamilyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    CREATE INDEX [IX_OAuthAuthCodes_ExpiresAt] ON [OAuthAuthCodes] ([ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827190000_AddAiConnectorOAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827190000_AddAiConnectorOAuth', N'8.0.10');
END;
GO

COMMIT;
GO

