-- ============================================================================
-- AddBluebeamExtraction  (2026-09-01)
-- ============================================================================
-- The Bluebeam drawing-data extraction schema: the single shared Studio
-- connection row (tokens live in SQL because refresh tokens rotate on use and
-- the api and worker share only the database), one extraction-status row per
-- drawing revision, the normalised per-markup table the data view renders
-- from, and Document Triage's archive-provenance column
-- (DocumentControlItems.SourceDocumentControlItemId).
-- Additive only, so it is safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260901090000_AddBluebeamExtraction.cs and records
-- itself in __EFMigrationsHistory so EF never re-applies it.
--
-- Apply with:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i add-bluebeam-extraction.sql -b -o migrate.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901090000_AddBluebeamExtraction'
)
BEGIN
    CREATE TABLE [BluebeamConnections] (
        [BluebeamConnectionId]   nvarchar(64)   NOT NULL,
        [RefreshToken]           nvarchar(2048) NOT NULL,
        [AccessToken]            nvarchar(2048) NULL,
        [AccessTokenExpiresAt]   datetimeoffset NULL,
        [ConnectedEmail]         nvarchar(256)  NOT NULL,
        [ConnectedBy]            nvarchar(256)  NOT NULL,
        [ConnectedAt]            datetimeoffset NOT NULL,
        [RefreshTokenUpdatedAt]  datetimeoffset NOT NULL,
        [LastRefreshSucceededAt] datetimeoffset NULL,
        [LastRefreshFailedAt]    datetimeoffset NULL,
        [LastRefreshError]       nvarchar(1024) NULL,
        CONSTRAINT [PK_BluebeamConnections] PRIMARY KEY ([BluebeamConnectionId])
    );

    CREATE TABLE [DrawingExtractions] (
        [DrawingExtractionId] nvarchar(64)   NOT NULL,
        [DrawingRevisionId]   nvarchar(64)   NOT NULL,
        [DrawingId]           nvarchar(64)   NOT NULL,
        [ProjectId]           nvarchar(64)   NOT NULL,
        [Status]              int            NOT NULL,
        [QueuedBy]            nvarchar(256)  NOT NULL,
        [QueuedAt]            datetimeoffset NOT NULL,
        [StartedAt]           datetimeoffset NULL,
        [CompletedAt]         datetimeoffset NULL,
        [Attempts]            int            NOT NULL,
        [ErrorMessage]        nvarchar(2048) NULL,
        [PageCount]           int            NULL,
        [PagesJson]           nvarchar(max)  NULL,
        [MarkupCount]         int            NULL,
        [MarkupsBlobRef]      nvarchar(1024) NULL,
        [TextBlobRef]         nvarchar(1024) NULL,
        [BluebeamSessionId]   nvarchar(128)  NULL,
        CONSTRAINT [PK_DrawingExtractions] PRIMARY KEY ([DrawingExtractionId])
    );

    CREATE UNIQUE INDEX [IX_DrawingExtractions_DrawingRevisionId]
        ON [DrawingExtractions] ([DrawingRevisionId]);
    CREATE INDEX [IX_DrawingExtractions_ProjectId_Status]
        ON [DrawingExtractions] ([ProjectId], [Status]);

    CREATE TABLE [DrawingMarkups] (
        [DrawingMarkupId]     nvarchar(64)   NOT NULL,
        [DrawingExtractionId] nvarchar(64)   NOT NULL,
        [DrawingRevisionId]   nvarchar(64)   NOT NULL,
        [BluebeamMarkupId]    nvarchar(128)  NOT NULL,
        [PageNumber]          int            NOT NULL,
        [MarkupType]          nvarchar(64)   NOT NULL,
        [Subject]             nvarchar(256)  NOT NULL,
        [Author]              nvarchar(256)  NOT NULL,
        [Comment]             nvarchar(4000) NOT NULL,
        [Colour]              nvarchar(32)   NOT NULL,
        [CreatedAtRaw]        nvarchar(64)   NOT NULL,
        [ModifiedAtRaw]       nvarchar(64)   NOT NULL,
        [MeasurementValue]    decimal(18,4)  NULL,
        [MeasurementUnit]     nvarchar(32)   NULL,
        [RectJson]            nvarchar(512)  NULL,
        [RawJson]             nvarchar(max)  NOT NULL,
        CONSTRAINT [PK_DrawingMarkups] PRIMARY KEY ([DrawingMarkupId])
    );

    CREATE INDEX [IX_DrawingMarkups_DrawingRevisionId]
        ON [DrawingMarkups] ([DrawingRevisionId]);
    CREATE INDEX [IX_DrawingMarkups_DrawingExtractionId]
        ON [DrawingMarkups] ([DrawingExtractionId]);

    IF COL_LENGTH('DocumentControlItems', 'SourceDocumentControlItemId') IS NULL
        ALTER TABLE [DocumentControlItems] ADD [SourceDocumentControlItemId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = N'IX_DocumentControlItems_SourceDocumentControlItemId'
      AND object_id = OBJECT_ID(N'[DocumentControlItems]')
)
BEGIN
    CREATE INDEX [IX_DocumentControlItems_SourceDocumentControlItemId]
        ON [DocumentControlItems] ([SourceDocumentControlItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901090000_AddBluebeamExtraction'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901090000_AddBluebeamExtraction', N'8.0.10');
END;
GO

COMMIT;
GO
