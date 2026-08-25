-- ============================================================================
-- AddAiAttachments  (2026-08-25)
-- ============================================================================
-- Files attached to an assistant conversation now keep their bytes (blob
-- container ai-attachments) so any part of them — the V01 tab of a forty-tab
-- valuation — can be read on demand, instead of the first 25,000 characters
-- being extracted once and the rest lost. This table points a conversation at
-- its blobs and holds each file's manifest. Additive only (a new table), so it
-- is safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260825120000_AddAiAttachments.cs and records itself
-- in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825120000_AddAiAttachments'
)
BEGIN
    CREATE TABLE [AiAttachments] (
        [AttachmentId]    nvarchar(64)   NOT NULL,
        [ConversationId]  nvarchar(64)   NOT NULL,
        [FileName]        nvarchar(256)  NOT NULL,
        [ContentType]     nvarchar(128)  NOT NULL,
        [SizeBytes]       bigint         NOT NULL,
        [BlobRef]         nvarchar(512)  NOT NULL,
        [ManifestJson]    nvarchar(max)  NOT NULL,
        [UploadedByEmail] nvarchar(256)  NOT NULL,
        [UploadedAt]      datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiAttachments] PRIMARY KEY ([AttachmentId])
    );

    CREATE INDEX [IX_AiAttachments_ConversationId] ON [AiAttachments] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825120000_AddAiAttachments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825120000_AddAiAttachments', N'8.0.10');
END;
GO

COMMIT;
GO
