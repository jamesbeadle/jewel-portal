BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160000_AddDefectNumbers'
)
BEGIN
    ALTER TABLE [Defects] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160000_AddDefectNumbers'
)
BEGIN

    WITH numbered AS (
        SELECT DefectId, ROW_NUMBER() OVER (ORDER BY RaisedAt, DefectId) AS rn
        FROM Defects
    )
    UPDATE d SET d.Number = n.rn
    FROM Defects d
    INNER JOIN numbered n ON n.DefectId = d.DefectId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160000_AddDefectNumbers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807160000_AddDefectNumbers', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807190000_AddWorkOrderAttachments'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[WorkOrderAttachments]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[WorkOrderAttachments] (
            [WorkOrderAttachmentId] nvarchar(64)   NOT NULL,
            [WorkOrderId]           nvarchar(64)   NOT NULL,
            [ProjectId]             nvarchar(64)   NOT NULL,
            [FileName]              nvarchar(256)  NOT NULL,
            [ContentType]           nvarchar(128)  NOT NULL,
            [FileSizeBytes]         bigint         NOT NULL,
            [BlobRef]               nvarchar(1024) NOT NULL,
            [Source]                int            NOT NULL,
            [AddedAt]               datetimeoffset NOT NULL,
            [AddedByEmail]          nvarchar(256)  NOT NULL,
            CONSTRAINT [PK_WorkOrderAttachments] PRIMARY KEY ([WorkOrderAttachmentId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807190000_AddWorkOrderAttachments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkOrderAttachments_WorkOrderId'
                   AND object_id = OBJECT_ID(N'[dbo].[WorkOrderAttachments]'))
        CREATE INDEX [IX_WorkOrderAttachments_WorkOrderId] ON [dbo].[WorkOrderAttachments] ([WorkOrderId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807190000_AddWorkOrderAttachments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807190000_AddWorkOrderAttachments', N'8.0.10');
END;
GO

COMMIT;
GO

