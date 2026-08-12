BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812090000_AddAiSkills'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[Skills]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[Skills] (
            [SkillKey]        nvarchar(128)  NOT NULL,
            [AgentKey]        nvarchar(64)   NOT NULL,
            [DisplayName]     nvarchar(256)  NOT NULL,
            [Description]     nvarchar(4000) NOT NULL,
            [Body]            nvarchar(max)  NOT NULL,
            [Pinned]          bit            NOT NULL,
            [IsActive]        bit            NOT NULL,
            [Version]         int            NOT NULL,
            [UpdatedByEmail]  nvarchar(256)  NOT NULL,
            [UpdatedAt]       datetimeoffset NOT NULL,
            CONSTRAINT [PK_Skills] PRIMARY KEY ([SkillKey])
        );

        CREATE INDEX [IX_Skills_AgentKey_IsActive]
            ON [dbo].[Skills] ([AgentKey], [IsActive]);
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812090000_AddAiSkills'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[SkillReferences]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[SkillReferences] (
            [SkillReferenceId] nvarchar(64)   NOT NULL,
            [SkillKey]         nvarchar(128)  NOT NULL,
            [RefKey]           nvarchar(128)  NOT NULL,
            [DisplayName]      nvarchar(256)  NOT NULL,
            [Description]      nvarchar(2000) NOT NULL,
            [Body]             nvarchar(max)  NOT NULL,
            [UpdatedByEmail]   nvarchar(256)  NOT NULL,
            [UpdatedAt]        datetimeoffset NOT NULL,
            CONSTRAINT [PK_SkillReferences] PRIMARY KEY ([SkillReferenceId])
        );

        CREATE UNIQUE INDEX [IX_SkillReferences_SkillKey_RefKey]
            ON [dbo].[SkillReferences] ([SkillKey], [RefKey]);
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812090000_AddAiSkills'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[SkillRevisions]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[SkillRevisions] (
            [SkillRevisionId] nvarchar(64)   NOT NULL,
            [SkillKey]        nvarchar(128)  NOT NULL,
            [Version]         int            NOT NULL,
            [Body]            nvarchar(max)  NOT NULL,
            [Description]     nvarchar(4000) NOT NULL,
            [SavedByEmail]    nvarchar(256)  NOT NULL,
            [SavedAt]         datetimeoffset NOT NULL,
            CONSTRAINT [PK_SkillRevisions] PRIMARY KEY ([SkillRevisionId])
        );

        CREATE INDEX [IX_SkillRevisions_SkillKey_Version]
            ON [dbo].[SkillRevisions] ([SkillKey], [Version]);
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812090000_AddAiSkills'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812090000_AddAiSkills', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812090000_AddBidPackageClosedAt'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [ClosedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812090000_AddBidPackageClosedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812090000_AddBidPackageClosedAt', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120000_AddBidPackageAttachmentsAndSpecSummary'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [SpecificationSummary] nvarchar(4000) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120000_AddBidPackageAttachmentsAndSpecSummary'
)
BEGIN
    CREATE TABLE [BidPackageAttachments] (
        [BidPackageAttachmentId] nvarchar(64) NOT NULL,
        [BidPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [FileName] nvarchar(256) NOT NULL,
        [ContentType] nvarchar(128) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [BlobRef] nvarchar(1024) NOT NULL,
        [Source] int NOT NULL,
        [AddedAt] datetimeoffset NOT NULL,
        [AddedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_BidPackageAttachments] PRIMARY KEY ([BidPackageAttachmentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120000_AddBidPackageAttachmentsAndSpecSummary'
)
BEGIN
    CREATE INDEX [IX_BidPackageAttachments_BidPackageId] ON [BidPackageAttachments] ([BidPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120000_AddBidPackageAttachmentsAndSpecSummary'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812120000_AddBidPackageAttachmentsAndSpecSummary', N'8.0.10');
END;
GO

COMMIT;
GO

