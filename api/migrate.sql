BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812180000_AddDocumentControl'
)
BEGIN
    CREATE TABLE [DocumentControlItems] (
        [DocumentControlItemId] nvarchar(64) NOT NULL,
        [MessageId] nvarchar(512) NOT NULL,
        [InternetMessageId] nvarchar(512) NULL,
        [AttachmentId] nvarchar(512) NOT NULL,
        [FromEmail] nvarchar(256) NOT NULL,
        [FromName] nvarchar(256) NOT NULL,
        [Subject] nvarchar(512) NOT NULL,
        [ReceivedAt] datetimeoffset NOT NULL,
        [FileName] nvarchar(256) NOT NULL,
        [ContentType] nvarchar(256) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [BlobRef] nvarchar(1024) NOT NULL,
        [ProjectIdHint] nvarchar(64) NULL,
        [Status] int NOT NULL,
        [SentBy] nvarchar(256) NOT NULL,
        [SentAt] datetimeoffset NOT NULL,
        [ResolvedBy] nvarchar(256) NULL,
        [ResolvedAt] datetimeoffset NULL,
        [FiledAsKind] int NULL,
        [FiledRecordId] nvarchar(64) NULL,
        [FiledLabel] nvarchar(512) NOT NULL,
        CONSTRAINT [PK_DocumentControlItems] PRIMARY KEY ([DocumentControlItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812180000_AddDocumentControl'
)
BEGIN
    CREATE INDEX [IX_DocumentControlItems_MessageId] ON [DocumentControlItems] ([MessageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812180000_AddDocumentControl'
)
BEGIN
    CREATE INDEX [IX_DocumentControlItems_Status] ON [DocumentControlItems] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812180000_AddDocumentControl'
)
BEGIN
    CREATE TABLE [PaymentCertificates] (
        [PaymentCertificateId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CertificateNumber] nvarchar(64) NOT NULL,
        [CertifiedAmount] decimal(18,2) NULL,
        [IssuedDate] datetimeoffset NOT NULL,
        [ValuationClaimId] nvarchar(64) NULL,
        [FileName] nvarchar(256) NOT NULL,
        [ContentType] nvarchar(256) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [BlobRef] nvarchar(1024) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [SourceDocumentControlItemId] nvarchar(64) NULL,
        CONSTRAINT [PK_PaymentCertificates] PRIMARY KEY ([PaymentCertificateId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812180000_AddDocumentControl'
)
BEGIN
    CREATE INDEX [IX_PaymentCertificates_ProjectId] ON [PaymentCertificates] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812180000_AddDocumentControl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812180000_AddDocumentControl', N'8.0.10');
END;
GO

COMMIT;
GO

