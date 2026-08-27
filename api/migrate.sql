BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE TABLE [BuildingControlCases] (
        [BuildingControlCaseId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Number] int NOT NULL,
        [Regime] int NOT NULL,
        [BodyName] nvarchar(256) NOT NULL,
        [BodyReference] nvarchar(128) NOT NULL,
        [ContactName] nvarchar(256) NOT NULL,
        [ContactEmail] nvarchar(256) NOT NULL,
        [ContactPhone] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [NoticeSubmittedOn] datetimeoffset NULL,
        [AcceptedOn] datetimeoffset NULL,
        [CompletionCertifiedOn] datetimeoffset NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BuildingControlCases] PRIMARY KEY ([BuildingControlCaseId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE TABLE [BuildingControlInspections] (
        [BuildingControlInspectionId] nvarchar(64) NOT NULL,
        [BuildingControlCaseId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Number] int NOT NULL,
        [StageName] nvarchar(256) NOT NULL,
        [Status] int NOT NULL,
        [BookedFor] datetimeoffset NULL,
        [InspectedAt] datetimeoffset NULL,
        [OutcomeNotes] nvarchar(2048) NOT NULL,
        [InspectorName] nvarchar(256) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [RaisedByEmail] nvarchar(256) NOT NULL,
        [RaisedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BuildingControlInspections] PRIMARY KEY ([BuildingControlInspectionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE TABLE [BuildingControlAttachments] (
        [BuildingControlAttachmentId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [BuildingControlCaseId] nvarchar(64) NULL,
        [BuildingControlInspectionId] nvarchar(64) NULL,
        [Kind] int NOT NULL,
        [FileName] nvarchar(512) NOT NULL,
        [ContentType] nvarchar(256) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [BlobRef] nvarchar(1024) NOT NULL,
        [Source] int NOT NULL,
        [AddedAt] datetimeoffset NOT NULL,
        [AddedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_BuildingControlAttachments] PRIMARY KEY ([BuildingControlAttachmentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlCases_ProjectId] ON [BuildingControlCases] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlCases_Number] ON [BuildingControlCases] ([Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlInspections_ProjectId] ON [BuildingControlInspections] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlInspections_BuildingControlCaseId] ON [BuildingControlInspections] ([BuildingControlCaseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlInspections_Number] ON [BuildingControlInspections] ([Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlAttachments_ProjectId] ON [BuildingControlAttachments] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlAttachments_BuildingControlCaseId] ON [BuildingControlAttachments] ([BuildingControlCaseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    CREATE INDEX [IX_BuildingControlAttachments_BuildingControlInspectionId] ON [BuildingControlAttachments] ([BuildingControlInspectionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827130000_AddBuildingControl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827130000_AddBuildingControl', N'8.0.10');
END;
GO

COMMIT;
GO

