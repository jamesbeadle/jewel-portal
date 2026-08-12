-- Applies 20260812180000_AddDocumentControl directly. Identical to what the scoped idempotent EF
-- script would emit for this migration: guarded tables + indexes, then the history row. Safe to
-- re-run. Until this runs, every Document Control page read 500s on the missing tables
-- ("Invalid object name 'DocumentControlItems'").
--
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i apply-document-control.sql -b -o apply-document-control.log

IF OBJECT_ID(N'[dbo].[DocumentControlItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DocumentControlItems] (
        [DocumentControlItemId] nvarchar(64)   NOT NULL,

        -- The source email: Graph ids while the mailbox still has it, envelope snapshot forever.
        [MessageId]             nvarchar(512)  NOT NULL,
        [InternetMessageId]     nvarchar(512)  NULL,
        [AttachmentId]          nvarchar(512)  NOT NULL,
        [FromEmail]             nvarchar(256)  NOT NULL,
        [FromName]              nvarchar(256)  NOT NULL,
        [Subject]               nvarchar(512)  NOT NULL,
        [ReceivedAt]            datetimeoffset NOT NULL,

        -- The file itself, held in the document-control blob store.
        [FileName]              nvarchar(256)  NOT NULL,
        [ContentType]           nvarchar(256)  NOT NULL,
        [FileSizeBytes]         bigint         NOT NULL,
        [BlobRef]               nvarchar(1024) NOT NULL,

        [ProjectIdHint]         nvarchar(64)   NULL,

        -- DocumentControlStatus: 0 Pending, 1 Filed, 2 Discarded.
        [Status]                int            NOT NULL,
        [SentBy]                nvarchar(256)  NOT NULL,
        [SentAt]                datetimeoffset NOT NULL,

        -- Stamped when the item is filed or discarded.
        [ResolvedBy]            nvarchar(256)  NULL,
        [ResolvedAt]            datetimeoffset NULL,
        [FiledAsKind]           int            NULL,
        [FiledRecordId]         nvarchar(64)   NULL,
        [FiledLabel]            nvarchar(512)  NOT NULL,
        CONSTRAINT [PK_DocumentControlItems] PRIMARY KEY ([DocumentControlItemId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentControlItems_MessageId'
               AND object_id = OBJECT_ID(N'[dbo].[DocumentControlItems]'))
    CREATE INDEX [IX_DocumentControlItems_MessageId] ON [dbo].[DocumentControlItems] ([MessageId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentControlItems_Status'
               AND object_id = OBJECT_ID(N'[dbo].[DocumentControlItems]'))
    CREATE INDEX [IX_DocumentControlItems_Status] ON [dbo].[DocumentControlItems] ([Status]);
GO

IF OBJECT_ID(N'[dbo].[PaymentCertificates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PaymentCertificates] (
        [PaymentCertificateId]        nvarchar(64)   NOT NULL,
        [ProjectId]                   nvarchar(64)   NOT NULL,
        [CertificateNumber]           nvarchar(64)   NOT NULL,
        [CertifiedAmount]             decimal(18,2)  NULL,
        [IssuedDate]                  datetimeoffset NOT NULL,
        [ValuationClaimId]            nvarchar(64)   NULL,

        [FileName]                    nvarchar(256)  NOT NULL,
        [ContentType]                 nvarchar(256)  NOT NULL,
        [FileSizeBytes]               bigint         NOT NULL,
        [BlobRef]                     nvarchar(1024) NOT NULL,

        [CreatedAt]                   datetimeoffset NOT NULL,
        [CreatedBy]                   nvarchar(256)  NOT NULL,
        -- Provenance: the Document Control item this certificate was filed from, when it came that way.
        [SourceDocumentControlItemId] nvarchar(64)   NULL,
        CONSTRAINT [PK_PaymentCertificates] PRIMARY KEY ([PaymentCertificateId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentCertificates_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[PaymentCertificates]'))
    CREATE INDEX [IX_PaymentCertificates_ProjectId] ON [dbo].[PaymentCertificates] ([ProjectId]);
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260812180000_AddDocumentControl')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812180000_AddDocumentControl', N'8.0.10');
GO
