-- ============================================================================
-- AddDrawingDataRows  (2026-09-16)
-- ============================================================================
-- The drawing transcription as rows: one table each for the figured dimensions,
-- callouts and closed shapes the extraction reads from a revision's PDF, so
-- work can be done over a drawing — filtered, paged, totalled, across a
-- project — without loading the structure blob; plus
-- DrawingExtractions.RowsWrittenAt, the stamp the rebuild uses to find
-- revisions extracted before the rows existed. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260916150000_AddDrawingDataRows.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api or worker reads the new tables. Every object guarded on its own.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-drawing-data-rows.sql -b -o add-drawing-data-rows.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916150000_AddDrawingDataRows')
BEGIN
    IF COL_LENGTH('DrawingExtractions', 'RowsWrittenAt') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [RowsWrittenAt] datetimeoffset NULL;

    IF OBJECT_ID(N'[DrawingDimensions]', N'U') IS NULL
    BEGIN
        CREATE TABLE [DrawingDimensions] (
            [DrawingDimensionId]  nvarchar(64) NOT NULL,
            [DrawingExtractionId] nvarchar(64) NOT NULL,
            [DrawingRevisionId]   nvarchar(64) NOT NULL,
            [DrawingId]           nvarchar(64) NOT NULL,
            [ProjectId]           nvarchar(64) NOT NULL,
            [Page]                int          NOT NULL,
            [ValueMm]             int          NOT NULL,
            [Axis]                nvarchar(1)  NOT NULL,
            [FromX]               float        NOT NULL,
            [FromY]               float        NOT NULL,
            [ToX]                 float        NOT NULL,
            [ToY]                 float        NOT NULL,
            [LabelX]              float        NOT NULL,
            [LabelY]              float        NOT NULL,
            CONSTRAINT [PK_DrawingDimensions] PRIMARY KEY ([DrawingDimensionId])
        );
        CREATE INDEX [IX_DrawingDimensions_DrawingRevisionId] ON [DrawingDimensions] ([DrawingRevisionId]);
        CREATE INDEX [IX_DrawingDimensions_ProjectId] ON [DrawingDimensions] ([ProjectId]);
    END;

    IF OBJECT_ID(N'[DrawingCallouts]', N'U') IS NULL
    BEGIN
        CREATE TABLE [DrawingCallouts] (
            [DrawingCalloutId]    nvarchar(64)   NOT NULL,
            [DrawingExtractionId] nvarchar(64)   NOT NULL,
            [DrawingRevisionId]   nvarchar(64)   NOT NULL,
            [DrawingId]           nvarchar(64)   NOT NULL,
            [ProjectId]           nvarchar(64)   NOT NULL,
            [Page]                int            NOT NULL,
            [X]                   float          NOT NULL,
            [Y]                   float          NOT NULL,
            [Text]                nvarchar(2000) NOT NULL,
            CONSTRAINT [PK_DrawingCallouts] PRIMARY KEY ([DrawingCalloutId])
        );
        CREATE INDEX [IX_DrawingCallouts_DrawingRevisionId] ON [DrawingCallouts] ([DrawingRevisionId]);
        CREATE INDEX [IX_DrawingCallouts_ProjectId] ON [DrawingCallouts] ([ProjectId]);
    END;

    IF OBJECT_ID(N'[DrawingShapes]', N'U') IS NULL
    BEGIN
        CREATE TABLE [DrawingShapes] (
            [DrawingShapeId]      nvarchar(64) NOT NULL,
            [DrawingExtractionId] nvarchar(64) NOT NULL,
            [DrawingRevisionId]   nvarchar(64) NOT NULL,
            [DrawingId]           nvarchar(64) NOT NULL,
            [ProjectId]           nvarchar(64) NOT NULL,
            [Page]                int          NOT NULL,
            [PointCount]          int          NOT NULL,
            [IsRectangle]         bit          NOT NULL,
            [HasCurves]           bit          NOT NULL,
            [CentreX]             float        NOT NULL,
            [CentreY]             float        NOT NULL,
            [WidthMm]             float        NOT NULL,
            [HeightMm]            float        NOT NULL,
            [PerimeterMm]         float        NOT NULL,
            [AreaSqM]             float        NOT NULL,
            CONSTRAINT [PK_DrawingShapes] PRIMARY KEY ([DrawingShapeId])
        );
        CREATE INDEX [IX_DrawingShapes_DrawingRevisionId] ON [DrawingShapes] ([DrawingRevisionId]);
        CREATE INDEX [IX_DrawingShapes_ProjectId] ON [DrawingShapes] ([ProjectId]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916150000_AddDrawingDataRows')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916150000_AddDrawingDataRows', N'8.0.10');
END;
GO

COMMIT;
GO
