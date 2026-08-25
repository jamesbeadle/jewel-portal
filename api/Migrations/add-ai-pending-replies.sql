-- ============================================================================
-- AddAiPendingReplies  (2026-08-25)
-- ============================================================================
-- A Claude call now runs on a background task with its own budget, and the
-- request that started it — or a later collect — reads the answer from this
-- table instead of waiting inside the Static Web Apps gateway's ~45s limit
-- (docs/ai/07-reply-collection.md). Additive only (a new table), so it is safe
-- to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260825160000_AddAiPendingReplies.cs and records
-- itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825160000_AddAiPendingReplies'
)
BEGIN
    CREATE TABLE [AiPendingReplies] (
        [ReplyId]        nvarchar(64)   NOT NULL,
        [ConversationId] nvarchar(64)   NOT NULL,
        [AfterSequence]  int            NOT NULL,
        [ModelTier]      nvarchar(32)   NULL,
        [Status]         int            NOT NULL,
        [ReplyJson]      nvarchar(max)  NULL,
        [Error]          nvarchar(64)   NULL,
        [RequestedAt]    datetimeoffset NOT NULL,
        [AnsweredAt]     datetimeoffset NULL,
        CONSTRAINT [PK_AiPendingReplies] PRIMARY KEY ([ReplyId])
    );

    CREATE INDEX [IX_AiPendingReplies_ConversationId_Status]
        ON [AiPendingReplies] ([ConversationId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825160000_AddAiPendingReplies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825160000_AddAiPendingReplies', N'8.0.10');
END;
GO

COMMIT;
GO
