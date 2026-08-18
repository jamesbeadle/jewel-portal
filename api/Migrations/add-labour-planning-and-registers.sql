BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [CompanyRegisterItems] (
        [RegisterItemId] nvarchar(64) NOT NULL,
        [Kind] int NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Counterparty] nvarchar(256) NOT NULL,
        [Reference] nvarchar(128) NOT NULL,
        [OwnerEmail] nvarchar(256) NOT NULL,
        [Cost] decimal(18,4) NOT NULL,
        [BillingCycle] nvarchar(64) NOT NULL,
        [KeyDate] datetimeoffset NULL,
        [SecondaryDate] datetimeoffset NULL,
        [Notes] nvarchar(2048) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_CompanyRegisterItems] PRIMARY KEY ([RegisterItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [CostCodeXeroMappings] (
        [CostCodeXeroMappingId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [XeroTrackingOptionId] nvarchar(64) NOT NULL,
        [XeroTrackingOptionName] nvarchar(256) NOT NULL,
        [LabourAccountCode] nvarchar(32) NOT NULL,
        [MaterialsAccountCode] nvarchar(32) NOT NULL,
        [TravelAccountCode] nvarchar(32) NOT NULL,
        [EffectiveFrom] datetimeoffset NOT NULL,
        [EffectiveTo] datetimeoffset NULL,
        CONSTRAINT [PK_CostCodeXeroMappings] PRIMARY KEY ([CostCodeXeroMappingId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [LabourWeekSignOffs] (
        [LabourWeekSignOffId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [WeekStart] datetimeoffset NOT NULL,
        [SignedOffByEmail] nvarchar(256) NOT NULL,
        [SignedOffAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_LabourWeekSignOffs] PRIMARY KEY ([LabourWeekSignOffId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [PolicyDocuments] (
        [PolicyDocumentId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Summary] nvarchar(max) NOT NULL,
        [Revision] int NOT NULL,
        [PublishedByEmail] nvarchar(256) NOT NULL,
        [PublishedAt] datetimeoffset NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_PolicyDocuments] PRIMARY KEY ([PolicyDocumentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [PolicySignOffs] (
        [PolicySignOffId] nvarchar(64) NOT NULL,
        [PolicyDocumentId] nvarchar(64) NOT NULL,
        [RecipientEmail] nvarchar(256) NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        [SignedAt] datetimeoffset NULL,
        [SignedName] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_PolicySignOffs] PRIMARY KEY ([PolicySignOffId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [SiteXeroMappings] (
        [SiteXeroMappingId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [XeroTrackingOptionId] nvarchar(64) NOT NULL,
        [XeroTrackingOptionName] nvarchar(256) NOT NULL,
        [EffectiveFrom] datetimeoffset NOT NULL,
        [EffectiveTo] datetimeoffset NULL,
        CONSTRAINT [PK_SiteXeroMappings] PRIMARY KEY ([SiteXeroMappingId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [WorkerAbsences] (
        [WorkerAbsenceId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [Date] datetimeoffset NOT NULL,
        [Kind] int NOT NULL,
        [Note] nvarchar(512) NOT NULL,
        [RecordedByEmail] nvarchar(256) NOT NULL,
        [RecordedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WorkerAbsences] PRIMARY KEY ([WorkerAbsenceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [WorkerCisStatuses] (
        [WorkerCisStatusId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [CisRatePercent] decimal(18,4) NOT NULL,
        [VerifiedRef] nvarchar(64) NOT NULL,
        [EffectiveFrom] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WorkerCisStatuses] PRIMARY KEY ([WorkerCisStatusId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [WorkerContracts] (
        [WorkerContractId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [ContractedDaysPerMonth] decimal(18,4) NOT NULL,
        [EffectiveFrom] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WorkerContracts] PRIMARY KEY ([WorkerContractId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [WorkerSettlementLines] (
        [WorkerSettlementLineId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [Month] datetimeoffset NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [Nature] int NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Note] nvarchar(512) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WorkerSettlementLines] PRIMARY KEY ([WorkerSettlementLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE TABLE [XeroCodingRuns] (
        [XeroCodingRunId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [Month] datetimeoffset NOT NULL,
        [Outcome] int NOT NULL,
        [XeroBillId] nvarchar(140) NOT NULL,
        [Detail] nvarchar(2048) NOT NULL,
        [RunByEmail] nvarchar(256) NOT NULL,
        [RunAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_XeroCodingRuns] PRIMARY KEY ([XeroCodingRunId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_CompanyRegisterItems_Kind] ON [CompanyRegisterItems] ([Kind]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_CostCodeXeroMappings_CostCode] ON [CostCodeXeroMappings] ([CostCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LabourWeekSignOffs_WorkerId_WeekStart] ON [LabourWeekSignOffs] ([WorkerId], [WeekStart]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PolicySignOffs_PolicyDocumentId_RecipientEmail] ON [PolicySignOffs] ([PolicyDocumentId], [RecipientEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_SiteXeroMappings_ProjectId] ON [SiteXeroMappings] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkerAbsences_WorkerId_Date] ON [WorkerAbsences] ([WorkerId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_WorkerCisStatuses_WorkerId] ON [WorkerCisStatuses] ([WorkerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_WorkerContracts_WorkerId] ON [WorkerContracts] ([WorkerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_WorkerSettlementLines_WorkerId_Month] ON [WorkerSettlementLines] ([WorkerId], [Month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    CREATE INDEX [IX_XeroCodingRuns_WorkerId_Month] ON [XeroCodingRuns] ([WorkerId], [Month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818093235_AddLabourPlanningAndRegisters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818093235_AddLabourPlanningAndRegisters', N'8.0.10');
END;
GO

COMMIT;
GO

