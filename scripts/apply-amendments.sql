-- Applies 20260811210000_AddProjectContractAmendments directly. Identical to what the scoped
-- idempotent EF script would emit for this migration: guarded table + index, then the history row.
-- Safe to re-run.
--
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i apply-amendments.sql -b -o apply-amendments.log

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
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectContractAmendments_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[ProjectContractAmendments]'))
    CREATE INDEX [IX_ProjectContractAmendments_ProjectId] ON [dbo].[ProjectContractAmendments] ([ProjectId]);
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260811210000_AddProjectContractAmendments')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811210000_AddProjectContractAmendments', N'8.0.10');
GO
