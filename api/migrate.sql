BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811210000_AddProjectContractAmendments'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[ProjectContractAmendments]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ProjectContractAmendments] (
            [ProjectContractAmendmentId] nvarchar(64)   NOT NULL,
            [ProjectId]                  nvarchar(64)   NOT NULL,

            [Title]                      nvarchar(256)  NOT NULL,
            [AmendmentDate]              datetimeoffset NULL,
            [Notes]                      nvarchar(4000) NULL,

            [DocumentBlobRef]            nvarchar(1024) NOT NULL,
            [DocumentFileName]           nvarchar(256)  NOT NULL,
            [DocumentContentType]        nvarchar(128)  NOT NULL,
            [DocumentFileSizeBytes]      bigint         NOT NULL,
            [DocumentUploadedAt]         datetimeoffset NOT NULL,
            [DocumentUploadedByEmail]    nvarchar(256)  NOT NULL,

            [UpdatedByEmail]             nvarchar(256)  NULL,
            [UpdatedAt]                  datetimeoffset NOT NULL,
            CONSTRAINT [PK_ProjectContractAmendments] PRIMARY KEY ([ProjectContractAmendmentId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811210000_AddProjectContractAmendments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectContractAmendments_ProjectId'
                   AND object_id = OBJECT_ID(N'[dbo].[ProjectContractAmendments]'))
        CREATE INDEX [IX_ProjectContractAmendments_ProjectId] ON [dbo].[ProjectContractAmendments] ([ProjectId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811210000_AddProjectContractAmendments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811210000_AddProjectContractAmendments', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811220000_AddUsefulInformationNotes'
)
BEGIN
    CREATE TABLE [UsefulInformationNotes] (
        [UsefulInformationNoteId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedByEmail] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_UsefulInformationNotes] PRIMARY KEY ([UsefulInformationNoteId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811220000_AddUsefulInformationNotes'
)
BEGIN
    CREATE INDEX [IX_UsefulInformationNotes_ProjectId] ON [UsefulInformationNotes] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811220000_AddUsefulInformationNotes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811220000_AddUsefulInformationNotes', N'8.0.10');
END;
GO

COMMIT;
GO

