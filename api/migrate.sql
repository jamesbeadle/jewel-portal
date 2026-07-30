IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [AccessRequests] (
        [Email] nvarchar(256) NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [Provider] int NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AccessRequests] PRIMARY KEY ([Email])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [BidDecisions] (
        [LeadId] nvarchar(64) NOT NULL,
        [ShouldBid] bit NOT NULL,
        [Reason] nvarchar(2048) NOT NULL,
        [DecidedByEmail] nvarchar(256) NOT NULL,
        [DecidedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BidDecisions] PRIMARY KEY ([LeadId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [BidPackages] (
        [BidPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Trade] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [OwnerEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_BidPackages] PRIMARY KEY ([BidPackageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [BoqLineItems] (
        [BoqLineItemId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [Unit] nvarchar(32) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [RateValue] decimal(18,4) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [Discipline] int NOT NULL,
        CONSTRAINT [PK_BoqLineItems] PRIMARY KEY ([BoqLineItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [BoqSignOffs] (
        [BoqSignOffId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [SignedOffByEmail] nvarchar(256) NOT NULL,
        [SignedOffAt] datetimeoffset NOT NULL,
        [TenderTotalAtSignOff] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_BoqSignOffs] PRIMARY KEY ([BoqSignOffId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [CashflowSnapshots] (
        [CashflowSnapshotId] nvarchar(64) NOT NULL,
        [GeneratedAt] datetimeoffset NOT NULL,
        [ExpectedIncome13Week] decimal(18,4) NOT NULL,
        [CommittedSpend13Week] decimal(18,4) NOT NULL,
        [NetPosition13Week] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_CashflowSnapshots] PRIMARY KEY ([CashflowSnapshotId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [ChangeRecords] (
        [ChangeRecordId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Kind] int NOT NULL,
        [Reference] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(2048) NOT NULL,
        [Status] int NOT NULL,
        [Value] decimal(18,4) NULL,
        [RaisedByEmail] nvarchar(256) NOT NULL,
        [RaisedAt] datetimeoffset NOT NULL,
        [RespondedAt] datetimeoffset NULL,
        [ResponseText] nvarchar(2048) NULL,
        [RespondedByEmail] nvarchar(256) NULL,
        [ImpliesVariation] bit NOT NULL,
        CONSTRAINT [PK_ChangeRecords] PRIMARY KEY ([ChangeRecordId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [ClaimPeriods] (
        [ClaimPeriodId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [PeriodNumber] int NOT NULL,
        [StartDate] datetimeoffset NOT NULL,
        [EndDate] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ClaimPeriods] PRIMARY KEY ([ClaimPeriodId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [ComplianceDocuments] (
        [ComplianceDocumentId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Kind] nvarchar(128) NOT NULL,
        [FileName] nvarchar(256) NOT NULL,
        [ExpiresAt] datetimeoffset NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ComplianceDocuments] PRIMARY KEY ([ComplianceDocumentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [ContraCharges] (
        [ContraChargeId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [SubcontractorReference] nvarchar(256) NOT NULL,
        [RaisedOn] datetimeoffset NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [Category] nvarchar(128) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [RecoveredAmount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ContraCharges] PRIMARY KEY ([ContraChargeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [CostCodeBudgets] (
        [CostCodeBudgetId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [AllocatedAmount] decimal(18,4) NOT NULL,
        [SpentAmount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_CostCodeBudgets] PRIMARY KEY ([CostCodeBudgetId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [CostCodes] (
        [CostCodeId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [Description] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_CostCodes] PRIMARY KEY ([CostCodeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [CvrPackageRows] (
        [CvrPackageRowId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [PackageName] nvarchar(256) NOT NULL,
        [OrderCost] decimal(18,4) NOT NULL,
        [OrderValue] decimal(18,4) NOT NULL,
        [VariationCost] decimal(18,4) NOT NULL,
        [VariationValue] decimal(18,4) NOT NULL,
        [MovementSinceLastSnapshot] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_CvrPackageRows] PRIMARY KEY ([CvrPackageRowId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [CvrSnapshots] (
        [CvrSnapshotId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [SnapshotAt] datetimeoffset NOT NULL,
        [TenderValue] decimal(18,4) NOT NULL,
        [ForecastFinalCost] decimal(18,4) NOT NULL,
        [ForecastFinalValue] decimal(18,4) NOT NULL,
        [MarginPounds] decimal(18,4) NOT NULL,
        [MarginPercent] decimal(18,4) NOT NULL,
        [WeeksAheadOrBehind] int NOT NULL,
        CONSTRAINT [PK_CvrSnapshots] PRIMARY KEY ([CvrSnapshotId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Dayworks] (
        [DayworkId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [WorkedOn] datetimeoffset NOT NULL,
        [SubcontractorReference] nvarchar(256) NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [InstructedBy] nvarchar(256) NOT NULL,
        [Hours] decimal(18,4) NOT NULL,
        [HourlyRate] decimal(18,4) NOT NULL,
        [LabourCost] decimal(18,4) NOT NULL,
        [PlantCost] decimal(18,4) NOT NULL,
        [MaterialsCost] decimal(18,4) NOT NULL,
        [UpliftPercent] decimal(18,4) NOT NULL,
        [ChargeableAmount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_Dayworks] PRIMARY KEY ([DayworkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Defects] (
        [DefectId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Description] nvarchar(1024) NOT NULL,
        [Location] nvarchar(256) NOT NULL,
        [AssignedToEmail] nvarchar(256) NOT NULL,
        [Status] int NOT NULL,
        [RaisedAt] datetimeoffset NOT NULL,
        [ResolvedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Defects] PRIMARY KEY ([DefectId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [DirectoryUserRoles] (
        [DirectoryUserRoleId] nvarchar(64) NOT NULL,
        [DirectoryUserEmail] nvarchar(256) NOT NULL,
        [Role] int NOT NULL,
        CONSTRAINT [PK_DirectoryUserRoles] PRIMARY KEY ([DirectoryUserRoleId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [DirectoryUsers] (
        [Email] nvarchar(256) NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_DirectoryUsers] PRIMARY KEY ([Email])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [DrawingIssueRecords] (
        [DrawingIssueRecordId] nvarchar(64) NOT NULL,
        [DrawingRevisionId] nvarchar(64) NOT NULL,
        [Source] nvarchar(64) NOT NULL,
        [IssuedByName] nvarchar(256) NOT NULL,
        [IssuedAt] datetimeoffset NOT NULL,
        [Notes] nvarchar(2048) NOT NULL,
        CONSTRAINT [PK_DrawingIssueRecords] PRIMARY KEY ([DrawingIssueRecordId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [DrawingRevisions] (
        [DrawingRevisionId] nvarchar(64) NOT NULL,
        [DrawingId] nvarchar(64) NOT NULL,
        [RevisionLabel] nvarchar(16) NOT NULL,
        [FileName] nvarchar(256) NOT NULL,
        [IssuedByEmail] nvarchar(256) NOT NULL,
        [ReceivedAt] datetimeoffset NOT NULL,
        [SupersededAt] datetimeoffset NULL,
        [IsAmbiguous] bit NOT NULL,
        [ViewCount] int NOT NULL,
        CONSTRAINT [PK_DrawingRevisions] PRIMARY KEY ([DrawingRevisionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Drawings] (
        [DrawingId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [DrawingCode] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [CurrentRevision] nvarchar(16) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Drawings] PRIMARY KEY ([DrawingId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Eots] (
        [EotId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Reason] nvarchar(1024) NOT NULL,
        [DaysGranted] int NOT NULL,
        [CommercialRecovery] decimal(18,4) NOT NULL,
        [GrantedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Eots] PRIMARY KEY ([EotId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [ForecastComponents] (
        [ForecastComponentId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [PackageName] nvarchar(128) NOT NULL,
        [CostIncurred] decimal(18,4) NOT NULL,
        [CostCommitted] decimal(18,4) NOT NULL,
        [QsAccrualAmount] decimal(18,4) NOT NULL,
        [PrelimForecast] decimal(18,4) NOT NULL,
        [CostToComplete] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ForecastComponents] PRIMARY KEY ([ForecastComponentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [HandoverPackItems] (
        [HandoverPackItemId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Label] nvarchar(256) NOT NULL,
        [Detail] nvarchar(1024) NOT NULL,
        [IsReady] bit NOT NULL,
        [EvidenceBlobRef] nvarchar(256) NULL,
        CONSTRAINT [PK_HandoverPackItems] PRIMARY KEY ([HandoverPackItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [HsRecordAttendance] (
        [HsRecordAttendanceId] nvarchar(64) NOT NULL,
        [HsRecordId] nvarchar(64) NOT NULL,
        [AttendeeName] nvarchar(256) NOT NULL,
        [SignatureBlobRef] nvarchar(256) NOT NULL,
        [SignedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_HsRecordAttendance] PRIMARY KEY ([HsRecordAttendanceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [HsRecords] (
        [HsRecordId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Kind] int NOT NULL,
        [Summary] nvarchar(512) NOT NULL,
        [Severity] int NOT NULL,
        [Status] int NOT NULL,
        [AssignedToEmail] nvarchar(256) NOT NULL,
        [RaisedAt] datetimeoffset NOT NULL,
        [DueAt] datetimeoffset NULL,
        [ClosedAt] datetimeoffset NULL,
        CONSTRAINT [PK_HsRecords] PRIMARY KEY ([HsRecordId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [InfoChaseItems] (
        [InfoChaseItemId] nvarchar(64) NOT NULL,
        [LeadId] nvarchar(64) NOT NULL,
        [Kind] nvarchar(32) NOT NULL,
        [Description] nvarchar(1024) NOT NULL,
        [IsReceived] bit NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InfoChaseItems] PRIMARY KEY ([InfoChaseItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [LeadOutcomes] (
        [LeadId] nvarchar(64) NOT NULL,
        [IsWon] bit NOT NULL,
        [Reason] nvarchar(2048) NOT NULL,
        [DecidedByEmail] nvarchar(256) NOT NULL,
        [DecidedAt] datetimeoffset NOT NULL,
        [CreatedProjectId] nvarchar(64) NULL,
        CONSTRAINT [PK_LeadOutcomes] PRIMARY KEY ([LeadId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Leads] (
        [LeadId] nvarchar(64) NOT NULL,
        [Reference] nvarchar(64) NOT NULL,
        [ContactName] nvarchar(256) NOT NULL,
        [ContactEmail] nvarchar(256) NOT NULL,
        [ContactPhone] nvarchar(64) NOT NULL,
        [CompanyName] nvarchar(256) NOT NULL,
        [SiteAddress] nvarchar(512) NOT NULL,
        [EstimatedValue] decimal(18,4) NULL,
        [Source] int NOT NULL,
        [Stage] int NOT NULL,
        [OwnerEmail] nvarchar(256) NOT NULL,
        [CapturedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Leads] PRIMARY KEY ([LeadId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [MobilisationItems] (
        [MobilisationItemId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [OwnerEmail] nvarchar(256) NOT NULL,
        [IsComplete] bit NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_MobilisationItems] PRIMARY KEY ([MobilisationItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Photos] (
        [PhotoId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [AttachedKind] int NOT NULL,
        [AttachedId] nvarchar(64) NULL,
        [BlobUri] nvarchar(1024) NOT NULL,
        [Caption] nvarchar(512) NOT NULL,
        [TakenByEmail] nvarchar(256) NOT NULL,
        [TakenAt] datetimeoffset NOT NULL,
        [GpsLatitude] decimal(18,4) NULL,
        [GpsLongitude] decimal(18,4) NULL,
        CONSTRAINT [PK_Photos] PRIMARY KEY ([PhotoId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [PracticalCompletions] (
        [PracticalCompletionId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [AchievedAt] datetimeoffset NOT NULL,
        [CertificateBlobRef] nvarchar(256) NULL,
        [IssuedByEmail] nvarchar(256) NOT NULL,
        [IsClientSigned] bit NOT NULL,
        CONSTRAINT [PK_PracticalCompletions] PRIMARY KEY ([PracticalCompletionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [PrelimForecastEntries] (
        [PrelimForecastEntryId] nvarchar(64) NOT NULL,
        [PrelimItemId] nvarchar(64) NOT NULL,
        [WeekNumber] int NOT NULL,
        [TenderedAmount] decimal(18,4) NOT NULL,
        [ActualAmount] decimal(18,4) NOT NULL,
        [ForecastAmount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_PrelimForecastEntries] PRIMARY KEY ([PrelimForecastEntryId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [PrelimItems] (
        [PrelimItemId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Description] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_PrelimItems] PRIMARY KEY ([PrelimItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [ProgrammeTasks] (
        [ProgrammeTaskId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [PlannedStart] datetimeoffset NOT NULL,
        [PlannedEnd] datetimeoffset NOT NULL,
        [ProgressPercent] decimal(18,4) NOT NULL,
        [BoqLineItemId] nvarchar(64) NULL,
        CONSTRAINT [PK_ProgrammeTasks] PRIMARY KEY ([ProgrammeTaskId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Projects] (
        [ProjectId] nvarchar(64) NOT NULL,
        [Reference] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [ClientName] nvarchar(256) NOT NULL,
        [Organisation] int NOT NULL,
        [Stage] int NOT NULL,
        [ProjectManagerEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([ProjectId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Proposals] (
        [ProposalId] nvarchar(64) NOT NULL,
        [LeadId] nvarchar(64) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [IssuedAt] datetimeoffset NOT NULL,
        [NegotiationRoundsJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Proposals] PRIMARY KEY ([ProposalId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [QsAccruals] (
        [QsAccrualId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Category] nvarchar(128) NOT NULL,
        [Description] nvarchar(1024) NOT NULL,
        [AddAmount] decimal(18,4) NOT NULL,
        [OmitAmount] decimal(18,4) NOT NULL,
        [LiabilityAmount] decimal(18,4) NOT NULL,
        [SignedOffByEmail] nvarchar(256) NOT NULL,
        [SignedOffAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_QsAccruals] PRIMARY KEY ([QsAccrualId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [QualificationAssessments] (
        [LeadId] nvarchar(64) NOT NULL,
        [Score] int NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [AssessedByEmail] nvarchar(256) NOT NULL,
        [AssessedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_QualificationAssessments] PRIMARY KEY ([LeadId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Quotes] (
        [QuoteId] nvarchar(64) NOT NULL,
        [BidPackageId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [Notes] nvarchar(1024) NOT NULL,
        [ReceivedAt] datetimeoffset NOT NULL,
        [IsDeclined] bit NOT NULL,
        CONSTRAINT [PK_Quotes] PRIMARY KEY ([QuoteId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Rates] (
        [RateId] nvarchar(64) NOT NULL,
        [Trade] nvarchar(64) NOT NULL,
        [Description] nvarchar(256) NOT NULL,
        [Unit] nvarchar(16) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [SupplierName] nvarchar(256) NOT NULL,
        [LastPricedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Rates] PRIMARY KEY ([RateId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [RetentionReleases] (
        [RetentionReleaseId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [ReleasedAt] datetimeoffset NOT NULL,
        [IsPublishedDownstream] bit NOT NULL,
        CONSTRAINT [PK_RetentionReleases] PRIMARY KEY ([RetentionReleaseId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [SettlementRecords] (
        [SettlementRecordId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [FinalContractValue] decimal(18,4) NOT NULL,
        [FinalCost] decimal(18,4) NOT NULL,
        [FinalMargin] decimal(18,4) NOT NULL,
        [AgreedAt] datetimeoffset NOT NULL,
        [IsClientSigned] bit NOT NULL,
        CONSTRAINT [PK_SettlementRecords] PRIMARY KEY ([SettlementRecordId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [SiteReports] (
        [SiteReportId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [PeriodEnd] datetimeoffset NOT NULL,
        [Narrative] nvarchar(max) NOT NULL,
        [AttendanceDays] int NOT NULL,
        [OpenSnags] int NOT NULL,
        [ProgressPercent] decimal(18,4) NOT NULL,
        [IsIssued] bit NOT NULL,
        CONSTRAINT [PK_SiteReports] PRIMARY KEY ([SiteReportId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [SiteVisits] (
        [SiteVisitId] nvarchar(64) NOT NULL,
        [LeadId] nvarchar(64) NOT NULL,
        [ScheduledAt] datetimeoffset NOT NULL,
        [AttendeeEmailsCsv] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [PhotoCount] int NOT NULL,
        [IsComplete] bit NOT NULL,
        CONSTRAINT [PK_SiteVisits] PRIMARY KEY ([SiteVisitId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [SubcontractorRetentions] (
        [SubcontractorRetentionId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [SubcontractorReference] nvarchar(256) NOT NULL,
        [CertifiedAmount] decimal(18,4) NOT NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [FirstReleasedAmount] decimal(18,4) NOT NULL,
        [FinalReleasedAmount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_SubcontractorRetentions] PRIMARY KEY ([SubcontractorRetentionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Subcontractors] (
        [SubcontractorId] nvarchar(64) NOT NULL,
        [CompanyName] nvarchar(256) NOT NULL,
        [PrimaryTrade] nvarchar(64) NOT NULL,
        [ContactName] nvarchar(256) NOT NULL,
        [ContactEmail] nvarchar(256) NOT NULL,
        [ContactPhone] nvarchar(64) NOT NULL,
        [CisStatus] nvarchar(32) NOT NULL,
        [OnboardedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Subcontractors] PRIMARY KEY ([SubcontractorId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Timesheets] (
        [TimesheetId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [PersonEmail] nvarchar(256) NOT NULL,
        [WorkedOn] datetimeoffset NOT NULL,
        [Hours] decimal(18,4) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [IsApproved] bit NOT NULL,
        CONSTRAINT [PK_Timesheets] PRIMARY KEY ([TimesheetId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [Valuations] (
        [ValuationId] nvarchar(64) NOT NULL,
        [ClaimPeriodId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [GrossValue] decimal(18,4) NOT NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [NetValue] decimal(18,4) NOT NULL,
        [IsIssued] bit NOT NULL,
        [IssuedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Valuations] PRIMARY KEY ([ValuationId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [VatAnalyses] (
        [VatAnalysisId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ZeroRatedAmount] decimal(18,4) NOT NULL,
        [StandardRatedAmount] decimal(18,4) NOT NULL,
        [Notes] nvarchar(2048) NOT NULL,
        [IsClientConfirmed] bit NOT NULL,
        [IsArchitectConfirmed] bit NOT NULL,
        CONSTRAINT [PK_VatAnalyses] PRIMARY KEY ([VatAnalysisId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [WalkRoundNotes] (
        [WalkRoundNoteId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [AuthorEmail] nvarchar(256) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [PhotoCount] int NOT NULL,
        [CapturedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WalkRoundNotes] PRIMARY KEY ([WalkRoundNoteId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    CREATE TABLE [WorkOrders] (
        [WorkOrderId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [BidPackageId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [Scope] nvarchar(1024) NOT NULL,
        [AwardedAt] datetimeoffset NOT NULL,
        [AwardedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_WorkOrders] PRIMARY KEY ([WorkOrderId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528221059_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260528221059_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624120000_AddLocalAuth'
)
BEGIN
    CREATE TABLE [UserCredentials] (
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(512) NULL,
        [Status] int NOT NULL,
        [FailedAttempts] int NOT NULL,
        [LockedUntil] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [PasswordSetAt] datetimeoffset NULL,
        CONSTRAINT [PK_UserCredentials] PRIMARY KEY ([Email])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624120000_AddLocalAuth'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [TokenHash] nvarchar(128) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Purpose] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [ConsumedAt] datetimeoffset NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([TokenHash])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624120000_AddLocalAuth'
)
BEGIN
    CREATE TABLE [UserSessions] (
        [SessionId] nvarchar(128) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([SessionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624120000_AddLocalAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260624120000_AddLocalAuth', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624122250_RemoveAuthProvider'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AccessRequests]') AND [c].[name] = N'Provider');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AccessRequests] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [AccessRequests] DROP COLUMN [Provider];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624122250_RemoveAuthProvider'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260624122250_RemoveAuthProvider', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    EXEC sp_rename N'[ChangeRecords].[ChangeRecordId]', N'RequestId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    EXEC sp_rename N'[ChangeRecords]', N'Requests';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [RaisedTo] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [DrawingRef] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [ResponseDue] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [RelatedDrawingSpec] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [InternalNotes] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [ClientNotes] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625090000_RenameChangesToRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625090000_RenameChangesToRequests', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626120000_AddRequestMessages'
)
BEGIN
    CREATE TABLE [RequestMessages] (
        [MessageId] nvarchar(64) NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [AuthorEmail] nvarchar(256) NOT NULL,
        [AuthorName] nvarchar(256) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Visibility] int NOT NULL,
        [PostedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_RequestMessages] PRIMARY KEY ([MessageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626120000_AddRequestMessages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626120000_AddRequestMessages', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    ALTER TABLE [RequestMessages] ADD [Direction] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    ALTER TABLE [RequestMessages] ADD [EmailMessageId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    ALTER TABLE [RequestMessages] ADD [InReplyTo] nvarchar(998) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    ALTER TABLE [RequestMessages] ADD [ConversationId] nvarchar(998) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    ALTER TABLE [RequestMessages] ADD [SentStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    CREATE TABLE [IntakeEmails] (
        [IntakeId] nvarchar(64) NOT NULL,
        [InternetMessageId] nvarchar(450) NOT NULL,
        [GraphMessageId] nvarchar(450) NULL,
        [ConversationId] nvarchar(998) NULL,
        [InReplyTo] nvarchar(998) NULL,
        [ReferencesHeader] nvarchar(max) NULL,
        [FromEmail] nvarchar(256) NOT NULL,
        [FromName] nvarchar(256) NOT NULL,
        [Subject] nvarchar(512) NOT NULL,
        [BodyPreview] nvarchar(4000) NOT NULL,
        [HasAttachments] bit NOT NULL,
        [ReceivedAt] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        [ClaimedByEmail] nvarchar(256) NULL,
        [ClaimedAt] datetimeoffset NULL,
        [LinkedRequestId] nvarchar(64) NULL,
        [Notes] nvarchar(512) NULL,
        CONSTRAINT [PK_IntakeEmails] PRIMARY KEY ([IntakeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626130000_AddRequestsMailboxIntake'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626130000_AddRequestsMailboxIntake', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626140000_AddMailboxSyncState'
)
BEGIN
    CREATE TABLE [MailboxSyncStates] (
        [Mailbox] nvarchar(256) NOT NULL,
        [DeltaLink] nvarchar(max) NULL,
        [LastSyncedAt] datetimeoffset NULL,
        [BacklogImported] bit NOT NULL,
        [SubscriptionId] nvarchar(450) NULL,
        [SubscriptionExpiresAt] datetimeoffset NULL,
        CONSTRAINT [PK_MailboxSyncStates] PRIMARY KEY ([Mailbox])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626140000_AddMailboxSyncState'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntakeEmails_InternetMessageId] ON [IntakeEmails] ([InternetMessageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626140000_AddMailboxSyncState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626140000_AddMailboxSyncState', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    CREATE TABLE [ValuationLineItems] (
        [ValuationLineItemId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ElementType] int NOT NULL,
        [SectionCode] nvarchar(16) NOT NULL,
        [SectionName] nvarchar(128) NOT NULL,
        [VariationRef] nvarchar(16) NOT NULL,
        [VariationTitle] nvarchar(256) NOT NULL,
        [LineType] int NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [Unit] nvarchar(16) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Rate] decimal(18,4) NOT NULL,
        [LineAmount] decimal(18,4) NOT NULL,
        [Comments] nvarchar(512) NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ValuationLineItems] PRIMARY KEY ([ValuationLineItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    CREATE TABLE [ValuationClaims] (
        [ValuationClaimId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ClaimNumber] int NOT NULL,
        [ClaimDate] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [RetentionReleasePercent] decimal(18,4) NOT NULL,
        [PreapprovedAt] datetimeoffset NULL,
        [ConfirmedAt] datetimeoffset NULL,
        [ContractSum] decimal(18,4) NOT NULL,
        [NetVariations] decimal(18,4) NOT NULL,
        [RevisedContractSum] decimal(18,4) NOT NULL,
        [TotalWorksComplete] decimal(18,4) NOT NULL,
        [RetentionHeld] decimal(18,4) NOT NULL,
        [RetentionReleased] decimal(18,4) NOT NULL,
        [CertifiedToDate] decimal(18,4) NOT NULL,
        [PaymentDueExVat] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ValuationClaims] PRIMARY KEY ([ValuationClaimId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    CREATE TABLE [ClaimLines] (
        [ClaimLineId] nvarchar(64) NOT NULL,
        [ValuationClaimId] nvarchar(64) NOT NULL,
        [ValuationLineItemId] nvarchar(64) NOT NULL,
        [PercentComplete] decimal(18,4) NOT NULL,
        [CumulativeClaimed] decimal(18,4) NOT NULL,
        [PeriodIncrement] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ClaimLines] PRIMARY KEY ([ClaimLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    CREATE INDEX [IX_ValuationLineItems_ProjectId] ON [ValuationLineItems] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    CREATE INDEX [IX_ValuationClaims_ProjectId] ON [ValuationClaims] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    CREATE INDEX [IX_ClaimLines_ValuationClaimId] ON [ClaimLines] ([ValuationClaimId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626150000_AddValuationReport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626150000_AddValuationReport', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627120000_AddRequestFolders'
)
BEGIN
    ALTER TABLE [Requests] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627120000_AddRequestFolders'
)
BEGIN
    ALTER TABLE [Requests] ADD [MailboxFolderId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627120000_AddRequestFolders'
)
BEGIN

    WITH numbered AS (
        SELECT RequestId,
               ROW_NUMBER() OVER (ORDER BY RaisedAt, RequestId) AS rn
        FROM Requests
    )
    UPDATE r
    SET r.Number = n.rn
    FROM Requests r
    INNER JOIN numbered n ON n.RequestId = r.RequestId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627120000_AddRequestFolders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260627120000_AddRequestFolders', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260628120000_AddProjectContacts'
)
BEGIN
    CREATE TABLE [ProjectContacts] (
        [ContactId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Organisation] nvarchar(256) NULL,
        [Role] int NOT NULL,
        [ReceivesRequests] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProjectContacts] PRIMARY KEY ([ContactId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260628120000_AddProjectContacts'
)
BEGIN
    CREATE INDEX [IX_ProjectContacts_ProjectId] ON [ProjectContacts] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260628120000_AddProjectContacts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260628120000_AddProjectContacts', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    CREATE TABLE [RequestAgents] (
        [RequestAgentId] nvarchar(64) NOT NULL,
        [AgentKey] nvarchar(64) NOT NULL,
        [AssignedAt] datetimeoffset NOT NULL,
        [AssignedByEmail] nvarchar(256) NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        [IsPrimary] bit NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [StatusMessage] nvarchar(1024) NOT NULL,
        CONSTRAINT [PK_RequestAgents] PRIMARY KEY ([RequestAgentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    CREATE TABLE [AgentChatMessages] (
        [MessageId] nvarchar(64) NOT NULL,
        [AgentKey] nvarchar(64) NOT NULL,
        [AuthorEmail] nvarchar(256) NOT NULL,
        [AuthorName] nvarchar(256) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [PostedAt] datetimeoffset NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [Role] int NOT NULL,
        CONSTRAINT [PK_AgentChatMessages] PRIMARY KEY ([MessageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    CREATE TABLE [AgentProposals] (
        [ProposalId] nvarchar(64) NOT NULL,
        [AgentKey] nvarchar(64) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [DecidedAt] datetimeoffset NULL,
        [DecidedByEmail] nvarchar(256) NULL,
        [Rationale] nvarchar(4000) NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [StructuredJson] nvarchar(max) NOT NULL,
        [Summary] nvarchar(1024) NOT NULL,
        CONSTRAINT [PK_AgentProposals] PRIMARY KEY ([ProposalId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    CREATE INDEX [IX_RequestAgents_RequestId] ON [RequestAgents] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    CREATE INDEX [IX_AgentChatMessages_RequestId_AgentKey] ON [AgentChatMessages] ([RequestId], [AgentKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    CREATE INDEX [IX_AgentProposals_RequestId] ON [AgentProposals] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629120000_AddRequestAgents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629120000_AddRequestAgents', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630120000_AddBidPackageInvites'
)
BEGIN
    CREATE TABLE [BidPackageRecipients] (
        [RecipientId] nvarchar(64) NOT NULL,
        [BidPackageId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [InvitedAt] datetimeoffset NOT NULL,
        [RespondedAt] datetimeoffset NULL,
        CONSTRAINT [PK_BidPackageRecipients] PRIMARY KEY ([RecipientId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630120000_AddBidPackageInvites'
)
BEGIN
    CREATE TABLE [BidPackageLineItems] (
        [LineItemId] nvarchar(64) NOT NULL,
        [BidPackageId] nvarchar(64) NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [Unit] nvarchar(32) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [Trade] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_BidPackageLineItems] PRIMARY KEY ([LineItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630120000_AddBidPackageInvites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630120000_AddBidPackageInvites', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [Category] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [MobileNumber] nvarchar(64) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [Town] nvarchar(128) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [County] nvarchar(128) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [Website] nvarchar(512) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [Pli] nvarchar(128) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [PliExpiry] nvarchar(64) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630130000_AddCompanyDirectory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630130000_AddCompanyDirectory', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    EXEC sp_rename N'[Drawings].[CurrentRevision]', N'CurrentApprovedRevisionLabel', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Drawings]') AND [c].[name] = N'CurrentApprovedRevisionLabel');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Drawings] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Drawings] ALTER COLUMN [CurrentApprovedRevisionLabel] nvarchar(16) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [ApprovalStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [BlobRef] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [ContentType] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [FileSizeBytes] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [ApprovedByEmail] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [ApprovedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN

    UPDATE [DrawingRevisions]
    SET [ApprovalStatus] = 1,
        [ApprovedAt] = [ReceivedAt],
        [ApprovedByEmail] = [IssuedByEmail]
    WHERE [SupersededAt] IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN

    UPDATE [DrawingRevisions]
    SET [ApprovalStatus] = 2
    WHERE [SupersededAt] IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701120000_AddDrawingApprovalAndBlob'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701120000_AddDrawingApprovalAndBlob', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701130000_AddClientAccountsAndRequestLadder'
)
BEGIN
    CREATE TABLE [Clients] (
        [ClientId] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [PrimaryContactName] nvarchar(256) NULL,
        [PrimaryContactEmail] nvarchar(256) NULL,
        [ArchitectName] nvarchar(256) NULL,
        [ArchitectEmail] nvarchar(256) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Clients] PRIMARY KEY ([ClientId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701130000_AddClientAccountsAndRequestLadder'
)
BEGIN
    ALTER TABLE [Requests] ADD [HasRfq] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701130000_AddClientAccountsAndRequestLadder'
)
BEGIN
    ALTER TABLE [Requests] ADD [ClientId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701130000_AddClientAccountsAndRequestLadder'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701130000_AddClientAccountsAndRequestLadder', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701140000_AddVariationOrderQuotes'
)
BEGIN
    CREATE TABLE [VariationOrderQuotes] (
        [VariationOrderQuoteId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [Number] int NOT NULL,
        [Reference] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(2048) NOT NULL,
        [Status] int NOT NULL,
        [SelectedBidPackageId] nvarchar(64) NULL,
        [SelectedSubcontractorId] nvarchar(64) NULL,
        [EstimatedValue] decimal(18,4) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [ApprovedAt] datetimeoffset NULL,
        [ApprovedByEmail] nvarchar(256) NULL,
        CONSTRAINT [PK_VariationOrderQuotes] PRIMARY KEY ([VariationOrderQuoteId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701140000_AddVariationOrderQuotes'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [VariationOrderQuoteId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701140000_AddVariationOrderQuotes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701140000_AddVariationOrderQuotes', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701150000_AddVariationOrders'
)
BEGIN
    CREATE TABLE [VariationOrders] (
        [VariationOrderId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [VariationOrderQuoteId] nvarchar(64) NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [Number] int NOT NULL,
        [VariationRef] nvarchar(16) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(2048) NOT NULL,
        [Status] int NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [SubcontractorId] nvarchar(64) NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [ApprovedAt] datetimeoffset NOT NULL,
        [ApprovedByEmail] nvarchar(256) NOT NULL,
        [IssuedAt] datetimeoffset NULL,
        [CancelledAt] datetimeoffset NULL,
        CONSTRAINT [PK_VariationOrders] PRIMARY KEY ([VariationOrderId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701150000_AddVariationOrders'
)
BEGIN
    ALTER TABLE [CostCodeBudgets] ADD [CommittedAmount] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701150000_AddVariationOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701150000_AddVariationOrders', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701160000_AddBidPackageReferenceAndLineCoverage'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701160000_AddBidPackageReferenceAndLineCoverage'
)
BEGIN

    WITH numbered AS (
        SELECT BidPackageId,
               ROW_NUMBER() OVER (ORDER BY CreatedAt, BidPackageId) AS rn
        FROM BidPackages
    )
    UPDATE bp
    SET bp.Number = n.rn
    FROM BidPackages bp
    INNER JOIN numbered n ON n.BidPackageId = bp.BidPackageId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701160000_AddBidPackageReferenceAndLineCoverage'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [Coverage] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701160000_AddBidPackageReferenceAndLineCoverage'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [BoqLineItemId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701160000_AddBidPackageReferenceAndLineCoverage'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [VariationOrderQuoteId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701160000_AddBidPackageReferenceAndLineCoverage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701160000_AddBidPackageReferenceAndLineCoverage', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701170000_AddCashCalls'
)
BEGIN
    CREATE TABLE [CashCalls] (
        [CashCallId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ValuationClaimId] nvarchar(64) NULL,
        [Number] int NOT NULL,
        [Reference] nvarchar(32) NOT NULL,
        [PeriodMonth] datetimeoffset NOT NULL,
        [AmountRequested] decimal(18,4) NOT NULL,
        [AmountReceived] decimal(18,4) NOT NULL,
        [Status] int NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        [InvoicedAt] datetimeoffset NULL,
        [ReceivedAt] datetimeoffset NULL,
        CONSTRAINT [PK_CashCalls] PRIMARY KEY ([CashCallId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701170000_AddCashCalls'
)
BEGIN
    ALTER TABLE [Projects] ADD [CashCallTotal] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701170000_AddCashCalls'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701170000_AddCashCalls', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702090000_AddTodoItems'
)
BEGIN
    CREATE TABLE [TodoItems] (
        [TodoItemId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Notes] nvarchar(2048) NOT NULL,
        [AssigneeEmail] nvarchar(256) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [IsComplete] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [DueAt] datetimeoffset NULL,
        [CompletedAt] datetimeoffset NULL,
        [Number] int NOT NULL,
        CONSTRAINT [PK_TodoItems] PRIMARY KEY ([TodoItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702090000_AddTodoItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702090000_AddTodoItems', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702120000_AddRequestReferenceUniqueIndex'
)
BEGIN

    WITH numbered AS (
        SELECT RequestId,
               ROW_NUMBER() OVER (PARTITION BY ProjectId, UPPER(Reference)
                                  ORDER BY RaisedAt, RequestId) AS rn
        FROM Requests
        WHERE Reference <> N''
    )
    UPDATE r
    SET r.Reference = r.Reference + N'-DUP' + CAST(n.rn - 1 AS NVARCHAR(8))
    FROM Requests r
    INNER JOIN numbered n ON n.RequestId = r.RequestId
    WHERE n.rn > 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702120000_AddRequestReferenceUniqueIndex'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Requests_Project_Reference] ON [Requests] ([ProjectId], [Reference]) WHERE [Reference] <> N''''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702120000_AddRequestReferenceUniqueIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702120000_AddRequestReferenceUniqueIndex', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702130000_ClearRequestAgents'
)
BEGIN
    DELETE FROM [AgentProposals];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702130000_ClearRequestAgents'
)
BEGIN
    DELETE FROM [AgentChatMessages];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702130000_ClearRequestAgents'
)
BEGIN
    DELETE FROM [RequestAgents];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702130000_ClearRequestAgents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702130000_ClearRequestAgents', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    CREATE TABLE [RequestItems] (
        [RequestItemId] nvarchar(64) NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [Position] int NOT NULL,
        [DrawingRef] nvarchar(1024) NOT NULL,
        [MemberArea] nvarchar(512) NOT NULL,
        [Query] nvarchar(4000) NOT NULL,
        [Response] nvarchar(4000) NULL,
        CONSTRAINT [PK_RequestItems] PRIMARY KEY ([RequestItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    CREATE INDEX [IX_RequestItems_RequestId] ON [RequestItems] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    ALTER TABLE [Requests] ADD [BasisOfQueries] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    ALTER TABLE [Requests] ADD [ResponseActionRequired] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    ALTER TABLE [Requests] ADD [ImpactIfLate] nvarchar(2048) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    ALTER TABLE [Projects] ADD [ClientId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    ALTER TABLE [Clients] ADD [RequestEmailPreference] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702150000_AddRfiFormAndClientAssignment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702150000_AddRfiFormAndClientAssignment', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    CREATE TABLE [Architects] (
        [ArchitectId] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [ContactName] nvarchar(256) NULL,
        [ContactEmail] nvarchar(256) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Architects] PRIMARY KEY ([ArchitectId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN

    INSERT INTO Architects (ArchitectId, Name, ContactName, ContactEmail, CreatedAt)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
           COALESCE(MAX(ArchitectName), ArchitectEmail),
           MAX(ArchitectName),
           ArchitectEmail,
           SYSDATETIMEOFFSET()
    FROM Clients
    WHERE ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(ArchitectEmail)) <> ''
    GROUP BY ArchitectEmail;

    INSERT INTO Architects (ArchitectId, Name, ContactName, ContactEmail, CreatedAt)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
           ArchitectName,
           ArchitectName,
           NULL,
           SYSDATETIMEOFFSET()
    FROM Clients
    WHERE (ArchitectEmail IS NULL OR LTRIM(RTRIM(ArchitectEmail)) = '')
      AND ArchitectName IS NOT NULL AND LTRIM(RTRIM(ArchitectName)) <> ''
    GROUP BY ArchitectName;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    EXEC sp_rename N'[Requests].[ClientId]', N'PartyId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    ALTER TABLE [Requests] ADD [PartyKind] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    ALTER TABLE [Requests] ADD [OnBehalfOfClientId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    EXEC sp_rename N'[Projects].[ClientId]', N'PartyId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    ALTER TABLE [Projects] ADD [PartyKind] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    ALTER TABLE [Projects] ADD [OnBehalfOfClientId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN

    UPDATE t
    SET t.PartyKind = 1,
        t.OnBehalfOfClientId = t.PartyId,
        t.PartyId = a.ArchitectId
    FROM Requests t
    JOIN Clients c ON c.ClientId = t.PartyId
    JOIN Architects a
      ON (c.ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(c.ArchitectEmail)) <> '' AND a.ContactEmail = c.ArchitectEmail)
      OR ((c.ArchitectEmail IS NULL OR LTRIM(RTRIM(c.ArchitectEmail)) = '') AND a.ContactEmail IS NULL AND a.Name = c.ArchitectName)
    WHERE (c.ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(c.ArchitectEmail)) <> '')
       OR (c.ArchitectName IS NOT NULL AND LTRIM(RTRIM(c.ArchitectName)) <> '');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN

    UPDATE t
    SET t.PartyKind = 1,
        t.OnBehalfOfClientId = t.PartyId,
        t.PartyId = a.ArchitectId
    FROM Projects t
    JOIN Clients c ON c.ClientId = t.PartyId
    JOIN Architects a
      ON (c.ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(c.ArchitectEmail)) <> '' AND a.ContactEmail = c.ArchitectEmail)
      OR ((c.ArchitectEmail IS NULL OR LTRIM(RTRIM(c.ArchitectEmail)) = '') AND a.ContactEmail IS NULL AND a.Name = c.ArchitectName)
    WHERE (c.ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(c.ArchitectEmail)) <> '')
       OR (c.ArchitectName IS NOT NULL AND LTRIM(RTRIM(c.ArchitectName)) <> '');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'ArchitectName');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Clients] DROP COLUMN [ArchitectName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'ArchitectEmail');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Clients] DROP COLUMN [ArchitectEmail];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'RequestEmailPreference');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Clients] DROP COLUMN [RequestEmailPreference];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702170000_SeparateArchitectsFromClients'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702170000_SeparateArchitectsFromClients', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[CashCalls]', N'ValuationInvoices';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[ValuationInvoices].[CashCallId]', N'ValuationInvoiceId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[ValuationInvoices].[AmountRequested]', N'Amount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[ValuationInvoices].[AmountReceived]', N'AmountPaid', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[ValuationInvoices].[RequestedAt]', N'RaisedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[ValuationInvoices].[InvoicedAt]', N'IssuedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[ValuationInvoices].[ReceivedAt]', N'PaidAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    EXEC sp_rename N'[Projects].[CashCallTotal]', N'ValuationInvoicePaidTotal', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    UPDATE ValuationInvoices SET Reference = REPLACE(Reference, 'CC-', 'VI-') WHERE Reference LIKE 'CC-%';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702190000_RenameCashCallsToValuationInvoices'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702190000_RenameCashCallsToValuationInvoices', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703090000_AddLadClaimsAndRequestNodLink'
)
BEGIN
    CREATE TABLE [LadClaims] (
        [LadClaimId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(2048) NOT NULL,
        [PeriodFrom] datetimeoffset NULL,
        [PeriodTo] datetimeoffset NULL,
        [DaysClaimed] int NOT NULL,
        [RatePerWeek] decimal(18,4) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Status] int NOT NULL,
        [RaisedAt] datetimeoffset NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [Number] int NOT NULL,
        CONSTRAINT [PK_LadClaims] PRIMARY KEY ([LadClaimId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703090000_AddLadClaimsAndRequestNodLink'
)
BEGIN
    ALTER TABLE [Requests] ADD [RelatedNodRequestId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703090000_AddLadClaimsAndRequestNodLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703090000_AddLadClaimsAndRequestNodLink', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706100000_AddQuoteLineItems'
)
BEGIN
    CREATE TABLE [QuoteLineItems] (
        [QuoteLineItemId] nvarchar(64) NOT NULL,
        [QuoteId] nvarchar(64) NOT NULL,
        [BidPackageLineItemId] nvarchar(64) NULL,
        [Description] nvarchar(512) NOT NULL,
        [Unit] nvarchar(32) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [Rate] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_QuoteLineItems] PRIMARY KEY ([QuoteLineItemId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706100000_AddQuoteLineItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706100000_AddQuoteLineItems', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706110000_AddBidPackageDrawings'
)
BEGIN
    CREATE TABLE [BidPackageDrawings] (
        [BidPackageDrawingId] nvarchar(64) NOT NULL,
        [BidPackageId] nvarchar(64) NOT NULL,
        [DrawingId] nvarchar(64) NOT NULL,
        [LinkedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_BidPackageDrawings] PRIMARY KEY ([BidPackageDrawingId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706110000_AddBidPackageDrawings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706110000_AddBidPackageDrawings', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN
    CREATE TABLE [PartyContacts] (
        [PartyContactId] nvarchar(64) NOT NULL,
        [PartyKind] int NOT NULL,
        [PartyId] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [JobTitle] nvarchar(256) NULL,
        [DefaultRouting] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PartyContacts] PRIMARY KEY ([PartyContactId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN
    CREATE INDEX [IX_PartyContacts_PartyKind_PartyId] ON [PartyContacts] ([PartyKind], [PartyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN

    INSERT INTO PartyContacts (PartyContactId, PartyKind, PartyId, Name, Email, JobTitle, DefaultRouting, IsPrimary, CreatedAt)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
           0,
           ClientId,
           COALESCE(NULLIF(LTRIM(RTRIM(PrimaryContactName)), ''), PrimaryContactEmail),
           LTRIM(RTRIM(PrimaryContactEmail)),
           NULL,
           1,
           1,
           SYSDATETIMEOFFSET()
    FROM Clients
    WHERE PrimaryContactEmail IS NOT NULL AND LTRIM(RTRIM(PrimaryContactEmail)) <> '';

    INSERT INTO PartyContacts (PartyContactId, PartyKind, PartyId, Name, Email, JobTitle, DefaultRouting, IsPrimary, CreatedAt)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
           1,
           ArchitectId,
           COALESCE(NULLIF(LTRIM(RTRIM(ContactName)), ''), ContactEmail),
           LTRIM(RTRIM(ContactEmail)),
           NULL,
           1,
           1,
           SYSDATETIMEOFFSET()
    FROM Architects
    WHERE ContactEmail IS NOT NULL AND LTRIM(RTRIM(ContactEmail)) <> '';

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN
    ALTER TABLE [ProjectContacts] ADD [Routing] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN
    ALTER TABLE [ProjectContacts] ADD [PartyContactId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN
    UPDATE ProjectContacts SET Routing = 1 WHERE ReceivesRequests = 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddCorrespondenceProfiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706150000_AddCorrespondenceProfiles', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddProgrammeLinksAndBaselines'
)
BEGIN
    CREATE TABLE [ProgrammeTaskLinks] (
        [ProgrammeTaskLinkId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [PredecessorTaskId] nvarchar(64) NOT NULL,
        [SuccessorTaskId] nvarchar(64) NOT NULL,
        [LagDays] int NOT NULL,
        CONSTRAINT [PK_ProgrammeTaskLinks] PRIMARY KEY ([ProgrammeTaskLinkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddProgrammeLinksAndBaselines'
)
BEGIN
    CREATE TABLE [ProgrammeBaselines] (
        [ProgrammeBaselineId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Label] nvarchar(256) NOT NULL,
        [TakenByEmail] nvarchar(256) NOT NULL,
        [TakenAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgrammeBaselines] PRIMARY KEY ([ProgrammeBaselineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddProgrammeLinksAndBaselines'
)
BEGIN
    CREATE TABLE [ProgrammeBaselineTasks] (
        [ProgrammeBaselineTaskId] nvarchar(64) NOT NULL,
        [ProgrammeBaselineId] nvarchar(64) NOT NULL,
        [ProgrammeTaskId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [PlannedStart] datetimeoffset NOT NULL,
        [PlannedEnd] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgrammeBaselineTasks] PRIMARY KEY ([ProgrammeBaselineTaskId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706150000_AddProgrammeLinksAndBaselines'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706150000_AddProgrammeLinksAndBaselines', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706170000_AddProjectAddress'
)
BEGIN
    ALTER TABLE [Projects] ADD [AddressLine] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706170000_AddProjectAddress'
)
BEGIN
    ALTER TABLE [Projects] ADD [Town] nvarchar(128) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706170000_AddProjectAddress'
)
BEGIN
    ALTER TABLE [Projects] ADD [Postcode] nvarchar(16) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706170000_AddProjectAddress'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706170000_AddProjectAddress', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN
    CREATE TABLE [Trades] (
        [TradeId] nvarchar(64) NOT NULL,
        [Name] nvarchar(64) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Trades] PRIMARY KEY ([TradeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Trades_Name] ON [Trades] ([Name]) WHERE [Name] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN
    CREATE TABLE [SubcontractorTrades] (
        [SubcontractorTradeId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [TradeId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_SubcontractorTrades] PRIMARY KEY ([SubcontractorTradeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SubcontractorTrades_SubcontractorId_TradeId] ON [SubcontractorTrades] ([SubcontractorId], [TradeId]) WHERE [SubcontractorId] IS NOT NULL AND [TradeId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN

    ;WITH split AS (
        SELECT LTRIM(RTRIM(part.value)) AS RawTrade
        FROM dbo.Subcontractors s
        CROSS APPLY STRING_SPLIT(s.PrimaryTrade, '/') AS part
        WHERE LTRIM(RTRIM(part.value)) <> N''
    ),
    canon AS (
        SELECT DISTINCT UPPER(LEFT(RawTrade, 1)) + SUBSTRING(RawTrade, 2, LEN(RawTrade)) AS TradeName
        FROM split
    )
    INSERT INTO dbo.Trades (TradeId, Name, CreatedAt)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), TradeName, SYSDATETIMEOFFSET()
    FROM canon;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN

    ;WITH split AS (
        SELECT s.SubcontractorId, LTRIM(RTRIM(part.value)) AS RawTrade
        FROM dbo.Subcontractors s
        CROSS APPLY STRING_SPLIT(s.PrimaryTrade, '/') AS part
        WHERE LTRIM(RTRIM(part.value)) <> N''
    ),
    canon AS (
        SELECT DISTINCT SubcontractorId,
               UPPER(LEFT(RawTrade, 1)) + SUBSTRING(RawTrade, 2, LEN(RawTrade)) AS TradeName
        FROM split
    )
    INSERT INTO dbo.SubcontractorTrades (SubcontractorTradeId, SubcontractorId, TradeId)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), canon.SubcontractorId, t.TradeId
    FROM canon
    JOIN dbo.Trades t ON t.Name = canon.TradeName;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Subcontractors]') AND [c].[name] = N'PrimaryTrade');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Subcontractors] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Subcontractors] DROP COLUMN [PrimaryTrade];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706180000_AddCuratedTrades'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706180000_AddCuratedTrades', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707150000_AddXeroLedger'
)
BEGIN
    CREATE TABLE [XeroLedgerLines] (
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [XeroInvoiceId] nvarchar(64) NOT NULL,
        [XeroLineItemId] nvarchar(64) NOT NULL,
        [Type] nvarchar(16) NOT NULL,
        [InvoiceNumber] nvarchar(64) NULL,
        [Reference] nvarchar(256) NULL,
        [ContactName] nvarchar(256) NULL,
        [Date] datetime2 NULL,
        [InvoiceStatus] nvarchar(32) NOT NULL,
        [Description] nvarchar(1024) NULL,
        [Net] decimal(18,4) NOT NULL,
        [Tax] decimal(18,4) NOT NULL,
        [AccountCode] nvarchar(32) NULL,
        [AccountName] nvarchar(256) NULL,
        [XeroSite] nvarchar(128) NULL,
        [XeroCostCode] nvarchar(128) NULL,
        [AllocationStatus] int NOT NULL,
        [ProjectId] nvarchar(64) NULL,
        [CostCenterCode] nvarchar(32) NULL,
        [AllocatedBy] nvarchar(256) NULL,
        [AllocatedAtUtc] datetimeoffset NULL,
        [Note] nvarchar(512) NULL,
        [FirstSeenAtUtc] datetimeoffset NOT NULL,
        [LastSyncedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_XeroLedgerLines] PRIMARY KEY ([XeroLedgerLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707150000_AddXeroLedger'
)
BEGIN
    CREATE INDEX [IX_XeroLedgerLines_AllocationStatus] ON [XeroLedgerLines] ([AllocationStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707150000_AddXeroLedger'
)
BEGIN
    CREATE INDEX [IX_XeroLedgerLines_ProjectId_CostCenterCode] ON [XeroLedgerLines] ([ProjectId], [CostCenterCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707150000_AddXeroLedger'
)
BEGIN
    CREATE INDEX [IX_XeroLedgerLines_XeroInvoiceId] ON [XeroLedgerLines] ([XeroInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707150000_AddXeroLedger'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260707150000_AddXeroLedger', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707200000_AddXeroLedgerBucket'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [Bucket] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707200000_AddXeroLedgerBucket'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260707200000_AddXeroLedgerBucket', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WorkOrders]') AND [c].[name] = N'BidPackageId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [WorkOrders] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [WorkOrders] ALTER COLUMN [BidPackageId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WorkOrders]') AND [c].[name] = N'Scope');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [WorkOrders] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [WorkOrders] ALTER COLUMN [Scope] nvarchar(4000) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [Title] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '2026-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [ScheduledCompletion] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [SourceReference] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    UPDATE WorkOrders SET CreatedAt = AwardedAt, Status = 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN

    WITH Numbered AS (
        SELECT WorkOrderId, ROW_NUMBER() OVER (ORDER BY AwardedAt, WorkOrderId) AS Rn
        FROM WorkOrders
    )
    UPDATE w SET w.Number = n.Rn
    FROM WorkOrders w
    JOIN Numbered n ON n.WorkOrderId = w.WorkOrderId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    CREATE TABLE [WorkOrderLines] (
        [WorkOrderLineId] nvarchar(64) NOT NULL,
        [WorkOrderId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(1024) NOT NULL,
        [CostType] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [LegacyCostCode] nvarchar(128) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(32) NOT NULL,
        [UnitCost] decimal(18,4) NOT NULL,
        [LineTotal] decimal(18,4) NOT NULL,
        [PaidToDate] decimal(18,4) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_WorkOrderLines] PRIMARY KEY ([WorkOrderLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260708130000_ExtendWorkOrdersForCostCenters', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    CREATE TABLE [XeroCostSplits] (
        [XeroCostSplitId] nvarchar(256) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCenterCode] nvarchar(32) NOT NULL,
        [Net] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_XeroCostSplits] PRIMARY KEY ([XeroCostSplitId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    CREATE INDEX [IX_XeroCostSplits_XeroLedgerLineId] ON [XeroCostSplits] ([XeroLedgerLineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    CREATE INDEX [IX_XeroCostSplits_ProjectId_CostCenterCode] ON [XeroCostSplits] ([ProjectId], [CostCenterCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [WriteBackStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [WriteBackError] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [WriteBackAtUtc] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    ALTER TABLE [Projects] ADD [XeroSiteName] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709120000_AddXeroCostSplitsAndWriteBack'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709120000_AddXeroCostSplitsAndWriteBack', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709150000_AddDrawingRevisionPipelineStatus'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [MetadataExtractedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709150000_AddDrawingRevisionPipelineStatus'
)
BEGIN
    ALTER TABLE [DrawingRevisions] ADD [AnalysedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709150000_AddDrawingRevisionPipelineStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709150000_AddDrawingRevisionPipelineStatus', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709170000_FixXeroCostSplitsProjectId'
)
BEGIN
    IF COL_LENGTH(N'XeroCostSplits', N'ProjectId') IS NULL
    BEGIN
        ALTER TABLE [XeroCostSplits]
            ADD [ProjectId] nvarchar(64) NOT NULL CONSTRAINT [DF_XeroCostSplits_ProjectId] DEFAULT N'';

        EXEC(N'UPDATE splits
               SET splits.[ProjectId] = ISNULL(lines.[ProjectId], N'''')
               FROM [XeroCostSplits] splits
               JOIN [XeroLedgerLines] lines ON lines.[XeroLedgerLineId] = splits.[XeroLedgerLineId]');

        ALTER TABLE [XeroCostSplits] DROP CONSTRAINT [DF_XeroCostSplits_ProjectId];
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709170000_FixXeroCostSplitsProjectId'
)
BEGIN
    IF COL_LENGTH(N'XeroCostSplits', N'XeroCostSplitId') < 512 -- COL_LENGTH is bytes; nvarchar(180) = 360
    BEGIN
        ALTER TABLE [XeroCostSplits] DROP CONSTRAINT [PK_XeroCostSplits];
        ALTER TABLE [XeroCostSplits] ALTER COLUMN [XeroCostSplitId] nvarchar(256) NOT NULL;
        ALTER TABLE [XeroCostSplits] ADD CONSTRAINT [PK_XeroCostSplits] PRIMARY KEY ([XeroCostSplitId]);
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709170000_FixXeroCostSplitsProjectId'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_XeroCostSplits_CostCenterCode' AND object_id = OBJECT_ID(N'XeroCostSplits'))
        DROP INDEX [IX_XeroCostSplits_CostCenterCode] ON [XeroCostSplits];

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_XeroCostSplits_ProjectId_CostCenterCode' AND object_id = OBJECT_ID(N'XeroCostSplits'))
        CREATE INDEX [IX_XeroCostSplits_ProjectId_CostCenterCode] ON [XeroCostSplits] ([ProjectId], [CostCenterCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709170000_FixXeroCostSplitsProjectId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709170000_FixXeroCostSplitsProjectId', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709190000_AddCostCentreCostProgress'
)
BEGIN
    CREATE TABLE [CostCentreCostProgress] (
        [CostCentreCostProgressId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [CostCompletionPercent] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_CostCentreCostProgress] PRIMARY KEY ([CostCentreCostProgressId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709190000_AddCostCentreCostProgress'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CostCentreCostProgress_ProjectId_CostCode] ON [CostCentreCostProgress] ([ProjectId], [CostCode]) WHERE [ProjectId] IS NOT NULL AND [CostCode] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709190000_AddCostCentreCostProgress'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709190000_AddCostCentreCostProgress', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709200000_AddCostCentreGroups'
)
BEGIN
    CREATE TABLE [CostCentreGroups] (
        [CostCentreGroupId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_CostCentreGroups] PRIMARY KEY ([CostCentreGroupId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709200000_AddCostCentreGroups'
)
BEGIN
    CREATE INDEX [IX_CostCentreGroups_ProjectId] ON [CostCentreGroups] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709200000_AddCostCentreGroups'
)
BEGIN
    CREATE TABLE [CostCentreGroupMembers] (
        [CostCentreGroupMemberId] nvarchar(64) NOT NULL,
        [CostCentreGroupId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        CONSTRAINT [PK_CostCentreGroupMembers] PRIMARY KEY ([CostCentreGroupMemberId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709200000_AddCostCentreGroups'
)
BEGIN
    CREATE INDEX [IX_CostCentreGroupMembers_CostCentreGroupId] ON [CostCentreGroupMembers] ([CostCentreGroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709200000_AddCostCentreGroups'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CostCentreGroupMembers_Project_CostCode] ON [CostCentreGroupMembers] ([ProjectId], [CostCode]) WHERE [ProjectId] IS NOT NULL AND [CostCode] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709200000_AddCostCentreGroups'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709200000_AddCostCentreGroups', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709210000_AddInvoiceWorkOrderLinksAndFinalisation'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [LinkedWorkOrderId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709210000_AddInvoiceWorkOrderLinksAndFinalisation'
)
BEGIN
    ALTER TABLE [CostCentreCostProgress] ADD [IsFinalised] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709210000_AddInvoiceWorkOrderLinksAndFinalisation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709210000_AddInvoiceWorkOrderLinksAndFinalisation', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710090000_AddRequestMergeLink'
)
BEGIN
    ALTER TABLE [Requests] ADD [MergedIntoRequestId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710090000_AddRequestMergeLink'
)
BEGIN
    ALTER TABLE [Requests] ADD [MergedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710090000_AddRequestMergeLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710090000_AddRequestMergeLink', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710120000_AddRequestClosedAt'
)
BEGIN
    ALTER TABLE [Requests] ADD [ClosedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710120000_AddRequestClosedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710120000_AddRequestClosedAt', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710150000_AddXeroLedgerHasAttachments'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [HasAttachments] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710150000_AddXeroLedgerHasAttachments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710150000_AddXeroLedgerHasAttachments', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [SubmittedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [ApprovedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [RejectedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [CancelledAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [RejectionReason] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [AmendmentCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [IsManual] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [ValuationReportSnapshotId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE TABLE [ValuationInvoiceEvents] (
        [ValuationInvoiceEventId] nvarchar(64) NOT NULL,
        [ValuationInvoiceId] nvarchar(64) NOT NULL,
        [EventType] int NOT NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        [Note] nvarchar(1024) NOT NULL,
        [AmountBefore] decimal(18,4) NULL,
        [AmountAfter] decimal(18,4) NULL,
        CONSTRAINT [PK_ValuationInvoiceEvents] PRIMARY KEY ([ValuationInvoiceEventId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE TABLE [ValuationReportSnapshots] (
        [ValuationReportSnapshotId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ValuationInvoiceId] nvarchar(64) NULL,
        [ValuationClaimId] nvarchar(64) NULL,
        [Label] nvarchar(256) NOT NULL,
        [TakenAt] datetimeoffset NOT NULL,
        [IsSuperseded] bit NOT NULL,
        [ContractSum] decimal(18,4) NOT NULL,
        [NetVariations] decimal(18,4) NOT NULL,
        [RevisedContractSum] decimal(18,4) NOT NULL,
        [TotalWorksComplete] decimal(18,4) NOT NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [RetentionHeld] decimal(18,4) NOT NULL,
        [RetentionReleasePercent] decimal(18,4) NOT NULL,
        [RetentionReleased] decimal(18,4) NOT NULL,
        [CertifiedToDate] decimal(18,4) NOT NULL,
        [PaymentDueExVat] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ValuationReportSnapshots] PRIMARY KEY ([ValuationReportSnapshotId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE TABLE [ValuationReportSnapshotLines] (
        [ValuationReportSnapshotLineId] nvarchar(64) NOT NULL,
        [ValuationReportSnapshotId] nvarchar(64) NOT NULL,
        [SourceValuationLineItemId] nvarchar(64) NOT NULL,
        [ElementType] int NOT NULL,
        [SectionCode] nvarchar(16) NOT NULL,
        [SectionName] nvarchar(128) NOT NULL,
        [VariationRef] nvarchar(16) NOT NULL,
        [VariationTitle] nvarchar(256) NOT NULL,
        [LineType] int NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [Description] nvarchar(512) NOT NULL,
        [Unit] nvarchar(16) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Rate] decimal(18,4) NOT NULL,
        [LineAmount] decimal(18,4) NOT NULL,
        [PercentComplete] decimal(18,4) NOT NULL,
        [CumulativeClaimed] decimal(18,4) NOT NULL,
        [PeriodIncrement] decimal(18,4) NOT NULL,
        [Comments] nvarchar(512) NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ValuationReportSnapshotLines] PRIMARY KEY ([ValuationReportSnapshotLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE INDEX [IX_ValuationInvoiceEvents_ValuationInvoiceId] ON [ValuationInvoiceEvents] ([ValuationInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE INDEX [IX_ValuationReportSnapshots_ProjectId] ON [ValuationReportSnapshots] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE INDEX [IX_ValuationReportSnapshots_ValuationInvoiceId] ON [ValuationReportSnapshots] ([ValuationInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    CREATE INDEX [IX_ValuationReportSnapshotLines_ValuationReportSnapshotId] ON [ValuationReportSnapshotLines] ([ValuationReportSnapshotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710160000_AddInvoiceApprovalAndReportSnapshots'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710160000_AddInvoiceApprovalAndReportSnapshots', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN
    CREATE TABLE [XeroLineWorkOrderLinks] (
        [XeroLineWorkOrderLinkId] nvarchar(64) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [WorkOrderId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_XeroLineWorkOrderLinks] PRIMARY KEY ([XeroLineWorkOrderLinkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_XeroLineWorkOrderLinks_Line_Order] ON [XeroLineWorkOrderLinks] ([XeroLedgerLineId], [WorkOrderId]) WHERE [XeroLedgerLineId] IS NOT NULL AND [WorkOrderId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN
    CREATE INDEX [IX_XeroLineWorkOrderLinks_WorkOrderId] ON [XeroLineWorkOrderLinks] ([WorkOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN
    CREATE INDEX [IX_XeroLineWorkOrderLinks_ProjectId] ON [XeroLineWorkOrderLinks] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN

    INSERT INTO XeroLineWorkOrderLinks (XeroLineWorkOrderLinkId, XeroLedgerLineId, WorkOrderId, ProjectId, Amount)
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')),
           XeroLedgerLineId,
           LinkedWorkOrderId,
           ProjectId,
           CASE WHEN [Type] = 'ACCPAYCREDIT' THEN -Net ELSE Net END
    FROM XeroLedgerLines
    WHERE LinkedWorkOrderId IS NOT NULL AND ProjectId IS NOT NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[XeroLedgerLines]') AND [c].[name] = N'LinkedWorkOrderId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [XeroLedgerLines] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [XeroLedgerLines] DROP COLUMN [LinkedWorkOrderId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710180000_AddXeroLineWorkOrderLinkSplits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710180000_AddXeroLineWorkOrderLinkSplits', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE TABLE [ReconciliationPackages] (
        [ReconciliationPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [IsLocked] bit NOT NULL DEFAULT CAST(0 AS bit),
        [LockedAt] datetimeoffset NULL,
        [LockedSalesValue] decimal(18,4) NOT NULL DEFAULT 0.0,
        [LockedClaimedToDate] decimal(18,4) NOT NULL DEFAULT 0.0,
        [LockedTargetCost] decimal(18,4) NOT NULL DEFAULT 0.0,
        [LockedWoCommitted] decimal(18,4) NOT NULL DEFAULT 0.0,
        [LockedInvoicedCost] decimal(18,4) NOT NULL DEFAULT 0.0,
        [LockedProfitLoss] decimal(18,4) NOT NULL DEFAULT 0.0,
        CONSTRAINT [PK_ReconciliationPackages] PRIMARY KEY ([ReconciliationPackageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackages_ProjectId] ON [ReconciliationPackages] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE TABLE [ReconciliationPackageOrders] (
        [ReconciliationPackageOrderId] nvarchar(64) NOT NULL,
        [ReconciliationPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [WorkOrderId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_ReconciliationPackageOrders] PRIMARY KEY ([ReconciliationPackageOrderId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackageOrders_ReconciliationPackageId] ON [ReconciliationPackageOrders] ([ReconciliationPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ReconciliationPackageOrders_Project_Order] ON [ReconciliationPackageOrders] ([ProjectId], [WorkOrderId]) WHERE [ProjectId] IS NOT NULL AND [WorkOrderId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE TABLE [ReconciliationPackageSalesLines] (
        [ReconciliationPackageSalesLineId] nvarchar(64) NOT NULL,
        [ReconciliationPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ValuationLineItemId] nvarchar(64) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ReconciliationPackageSalesLines] PRIMARY KEY ([ReconciliationPackageSalesLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackageSalesLines_ReconciliationPackageId] ON [ReconciliationPackageSalesLines] ([ReconciliationPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ReconciliationPackageSalesLines_Package_Line] ON [ReconciliationPackageSalesLines] ([ReconciliationPackageId], [ValuationLineItemId]) WHERE [ReconciliationPackageId] IS NOT NULL AND [ValuationLineItemId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackageSalesLines_ValuationLineItemId] ON [ReconciliationPackageSalesLines] ([ValuationLineItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711120000_AddReconciliationPackages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260711120000_AddReconciliationPackages', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [WorkerId] nvarchar(64) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [SiteAttendanceId] nvarchar(64) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [RateApplied] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [CostAmount] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [ApprovedByEmail] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [ApprovedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    ALTER TABLE [Timesheets] ADD [RejectionReason] nvarchar(1024) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    UPDATE Timesheets SET Status = 1 WHERE IsApproved = 1
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [Workers] (
        [WorkerId] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [SubcontractorId] nvarchar(64) NULL,
        [HourlyRate] decimal(18,4) NOT NULL,
        [IsActive] bit NOT NULL,
        [ContactEmail] nvarchar(256) NOT NULL,
        [ContactPhone] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_Workers] PRIMARY KEY ([WorkerId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [WorkerRateHistories] (
        [WorkerRateHistoryId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [HourlyRate] decimal(18,4) NOT NULL,
        [EffectiveFrom] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WorkerRateHistories] PRIMARY KEY ([WorkerRateHistoryId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [ProjectWorkerAssignments] (
        [ProjectWorkerAssignmentId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ProjectWorkerAssignments] PRIMARY KEY ([ProjectWorkerAssignmentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [SiteAttendances] (
        [SiteAttendanceId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [WorkerId] nvarchar(64) NOT NULL,
        [WorkDate] datetimeoffset NOT NULL,
        [SignedInAt] datetimeoffset NOT NULL,
        [SignedOutAt] datetimeoffset NULL,
        CONSTRAINT [PK_SiteAttendances] PRIMARY KEY ([SiteAttendanceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [SiteAccessTokens] (
        [SiteAccessTokenId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Token] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_SiteAccessTokens] PRIMARY KEY ([SiteAccessTokenId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [XeroLineTimesheetCovers] (
        [XeroLineTimesheetCoverId] nvarchar(64) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [PeriodStart] datetimeoffset NOT NULL,
        [PeriodEnd] datetimeoffset NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_XeroLineTimesheetCovers] PRIMARY KEY ([XeroLineTimesheetCoverId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    CREATE TABLE [LabourSettlementVariances] (
        [LabourSettlementVarianceId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Reason] nvarchar(1024) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_LabourSettlementVariances] PRIMARY KEY ([LabourSettlementVarianceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddLabourTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713100000_AddLabourTracking', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddSubcontractorUserLink'
)
BEGIN
    ALTER TABLE [DirectoryUsers] ADD [SubcontractorId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713100000_AddSubcontractorUserLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713100000_AddSubcontractorUserLink', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713110000_AddComplianceDocumentFiles'
)
BEGIN
    ALTER TABLE [ComplianceDocuments] ADD [BlobPath] nvarchar(1024) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713110000_AddComplianceDocumentFiles'
)
BEGIN
    ALTER TABLE [ComplianceDocuments] ADD [ContentType] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713110000_AddComplianceDocumentFiles'
)
BEGIN
    ALTER TABLE [ComplianceDocuments] ADD [FileSize] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713110000_AddComplianceDocumentFiles'
)
BEGIN
    ALTER TABLE [ComplianceDocuments] ADD [Version] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713110000_AddComplianceDocumentFiles'
)
BEGIN
    ALTER TABLE [ComplianceDocuments] ADD [SupersededAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713110000_AddComplianceDocumentFiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713110000_AddComplianceDocumentFiles', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713120000_AddSubcontractorVariationRequests'
)
BEGIN
    CREATE TABLE [SubcontractorVariationRequests] (
        [VariationRequestId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [WorkOrderId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(2048) NOT NULL,
        [ProposedValue] decimal(18,4) NOT NULL,
        [Status] int NOT NULL,
        [SubmittedAt] datetimeoffset NOT NULL,
        [ReviewedAt] datetimeoffset NULL,
        [ReviewedByEmail] nvarchar(256) NULL,
        [RejectionReason] nvarchar(1024) NOT NULL,
        [VariationOrderQuoteId] nvarchar(64) NULL,
        CONSTRAINT [PK_SubcontractorVariationRequests] PRIMARY KEY ([VariationRequestId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713120000_AddSubcontractorVariationRequests'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [VariationOrderId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713120000_AddSubcontractorVariationRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713120000_AddSubcontractorVariationRequests', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713150000_DropSiteAccessTokens'
)
BEGIN
    DROP TABLE [SiteAccessTokens];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260713150000_DropSiteAccessTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713150000_DropSiteAccessTokens', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714090000_AddProjectNextValuationDate'
)
BEGIN
    ALTER TABLE [Projects] ADD [NextExpectedValuationDate] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714090000_AddProjectNextValuationDate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260714090000_AddProjectNextValuationDate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715090000_AddRequestIssuedAt'
)
BEGIN
    ALTER TABLE [Requests] ADD [IssuedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715090000_AddRequestIssuedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715090000_AddRequestIssuedAt', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715120000_AddProjectRetention'
)
BEGIN
    CREATE TABLE [ProjectRetentions] (
        [ProjectRetentionId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [CompletionReleasePercent] decimal(18,4) NOT NULL,
        [DefectsPeriodMonths] int NOT NULL,
        [PracticalCompletionAt] datetimeoffset NULL,
        [CompletionReleaseConfirmedAt] datetimeoffset NULL,
        [CompletionReleaseAmount] decimal(18,4) NOT NULL,
        [FinalReleaseConfirmedAt] datetimeoffset NULL,
        [FinalReleaseAmount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ProjectRetentions] PRIMARY KEY ([ProjectRetentionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715120000_AddProjectRetention'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProjectRetentions_ProjectId] ON [ProjectRetentions] ([ProjectId]) WHERE [ProjectId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715120000_AddProjectRetention'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715120000_AddProjectRetention', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE TABLE [ProgressUpdates] (
        [ProgressUpdateId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [WorkDate] datetimeoffset NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgressUpdates] PRIMARY KEY ([ProgressUpdateId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE TABLE [ProgressPhotos] (
        [ProgressPhotoId] nvarchar(64) NOT NULL,
        [ProgressUpdateId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [FileName] nvarchar(512) NOT NULL,
        [BlobRef] nvarchar(1024) NOT NULL,
        [ContentType] nvarchar(256) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [SortOrder] int NOT NULL,
        [UploadedByEmail] nvarchar(256) NOT NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgressPhotos] PRIMARY KEY ([ProgressPhotoId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE TABLE [ProgressReports] (
        [ProgressReportId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [PeriodStart] datetimeoffset NULL,
        [PeriodEnd] datetimeoffset NULL,
        [Introduction] nvarchar(max) NOT NULL,
        [WorkCompleted] nvarchar(max) NOT NULL,
        [UpcomingWorks] nvarchar(max) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgressReports] PRIMARY KEY ([ProgressReportId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE TABLE [ProgressReportSelections] (
        [ProgressReportSelectionId] nvarchar(64) NOT NULL,
        [ProgressReportId] nvarchar(64) NOT NULL,
        [ProgressUpdateId] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProgressReportSelections] PRIMARY KEY ([ProgressReportSelectionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE INDEX [IX_ProgressUpdates_ProjectId] ON [ProgressUpdates] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE INDEX [IX_ProgressPhotos_ProgressUpdateId] ON [ProgressPhotos] ([ProgressUpdateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE INDEX [IX_ProgressPhotos_ProjectId] ON [ProgressPhotos] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE INDEX [IX_ProgressReports_ProjectId] ON [ProgressReports] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    CREATE INDEX [IX_ProgressReportSelections_ProgressReportId] ON [ProgressReportSelections] ([ProgressReportId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715150000_AddProgressReports'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715150000_AddProgressReports', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715160000_AddBidPackageLineCostCodesAndMaterials'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [CostCode] nvarchar(32) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715160000_AddBidPackageLineCostCodesAndMaterials'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [MaterialsApplicable] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715160000_AddBidPackageLineCostCodesAndMaterials'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715160000_AddBidPackageLineCostCodesAndMaterials', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherSummary] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherObservedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherTempHighC] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherTempLowC] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherWindMph] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherHumidityPercent] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    ALTER TABLE [ProgressUpdates] ADD [WeatherPrecipInches] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716100000_AddProgressUpdateWeather'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716100000_AddProgressUpdateWeather', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716110000_AddReconciliationPackageCostLines'
)
BEGIN
    CREATE TABLE [ReconciliationPackageCostLines] (
        [ReconciliationPackageCostLineId] nvarchar(64) NOT NULL,
        [ReconciliationPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [Amount] decimal(18,4) NOT NULL DEFAULT 0.0,
        CONSTRAINT [PK_ReconciliationPackageCostLines] PRIMARY KEY ([ReconciliationPackageCostLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716110000_AddReconciliationPackageCostLines'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackageCostLines_ReconciliationPackageId] ON [ReconciliationPackageCostLines] ([ReconciliationPackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716110000_AddReconciliationPackageCostLines'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackageCostLines_ProjectId] ON [ReconciliationPackageCostLines] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716110000_AddReconciliationPackageCostLines'
)
BEGIN
    CREATE INDEX [IX_ReconciliationPackageCostLines_XeroLedgerLineId] ON [ReconciliationPackageCostLines] ([XeroLedgerLineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716110000_AddReconciliationPackageCostLines'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716110000_AddReconciliationPackageCostLines', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120000_AddRequestRaisedToContact'
)
BEGIN
    ALTER TABLE [Requests] ADD [RaisedToContactId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120000_AddRequestRaisedToContact'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716120000_AddRequestRaisedToContact', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120000_TodoItemsAssignToRole'
)
BEGIN
    ALTER TABLE [TodoItems] ADD [AssigneeRole] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120000_TodoItemsAssignToRole'
)
BEGIN

    UPDATE t
    SET AssigneeRole = best.Role
    FROM TodoItems t
    CROSS APPLY (
        SELECT TOP 1 dur.Role
        FROM DirectoryUserRoles dur
        WHERE LOWER(dur.DirectoryUserEmail) = LOWER(t.AssigneeEmail)
          AND dur.Role IN (0, 1, 2, 3, 4, 5, 6, 7)
        ORDER BY CASE WHEN dur.Role = 0 THEN 99 ELSE dur.Role END
    ) best
    WHERE t.AssigneeEmail <> '';

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120000_TodoItemsAssignToRole'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TodoItems]') AND [c].[name] = N'AssigneeEmail');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [TodoItems] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [TodoItems] DROP COLUMN [AssigneeEmail];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120000_TodoItemsAssignToRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716120000_TodoItemsAssignToRole', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130000_AddRequestCriticalPath'
)
BEGIN
    ALTER TABLE [Requests] ADD [CriticalPath] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130000_AddRequestCriticalPath'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716130000_AddRequestCriticalPath', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716140000_BackfillRequestIssuedAt'
)
BEGIN
    UPDATE [Requests] SET [IssuedAt] = [RaisedAt] WHERE [IssuedAt] IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716140000_BackfillRequestIssuedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716140000_BackfillRequestIssuedAt', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721090000_AddValuationClaimName'
)
BEGIN
    ALTER TABLE [ValuationClaims] ADD [Name] nvarchar(128) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721090000_AddValuationClaimName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721090000_AddValuationClaimName', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721100000_RecomputeLockedPackageProfitToSales'
)
BEGIN
    UPDATE [ReconciliationPackages] SET [LockedProfitLoss] = [LockedSalesValue] - [LockedInvoicedCost] WHERE [IsLocked] = 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721100000_RecomputeLockedPackageProfitToSales'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721100000_RecomputeLockedPackageProfitToSales', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722100000_AddAuditEvents'
)
BEGIN
    CREATE TABLE [AuditEvents] (
        [AuditEventId] nvarchar(64) NOT NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        [ActorEmail] nvarchar(256) NOT NULL,
        [EventType] int NOT NULL,
        [Pathway] nvarchar(32) NOT NULL,
        [ProjectId] nvarchar(64) NULL,
        [RecordType] int NULL,
        [RecordId] nvarchar(64) NULL,
        [RecordReference] nvarchar(64) NOT NULL,
        [ConversationId] nvarchar(512) NULL,
        [EmailMessageId] nvarchar(512) NULL,
        [InternetMessageId] nvarchar(512) NULL,
        [WebLink] nvarchar(1024) NULL,
        [Detail] nvarchar(1024) NOT NULL,
        CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([AuditEventId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722100000_AddAuditEvents'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_OccurredAt] ON [AuditEvents] ([OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722100000_AddAuditEvents'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_ProjectId] ON [AuditEvents] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722100000_AddAuditEvents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722100000_AddAuditEvents', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723090000_ConsolidateRequestStatuses'
)
BEGIN
    UPDATE [Requests] SET [ClosedAt] = COALESCE([ClosedAt], [RespondedAt], [IssuedAt], [RaisedAt]), [Status] = 4 WHERE [Status] IN (2, 3, 5);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723090000_ConsolidateRequestStatuses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723090000_ConsolidateRequestStatuses', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723110000_AddWorkOrderProgrammeAndAcceptance'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [ProgrammeStart] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723110000_AddWorkOrderProgrammeAndAcceptance'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [ProgrammeNotes] nvarchar(2000) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723110000_AddWorkOrderProgrammeAndAcceptance'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [AcceptedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723110000_AddWorkOrderProgrammeAndAcceptance'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [AcceptedByEmail] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723110000_AddWorkOrderProgrammeAndAcceptance'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [AcceptedByName] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723110000_AddWorkOrderProgrammeAndAcceptance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723110000_AddWorkOrderProgrammeAndAcceptance', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [VariationRef] nvarchar(16) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [Value] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [CostCode] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [IssuedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [RejectedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN

    UPDATE q SET
        q.[VariationRef]            = vo.[VariationRef],
        q.[Value]                   = vo.[Value],
        q.[CostCode]                = vo.[CostCode],
        q.[IssuedAt]                = vo.[IssuedAt],
        q.[RejectedAt]              = vo.[CancelledAt],
        q.[ApprovedAt]              = COALESCE(q.[ApprovedAt], vo.[ApprovedAt]),
        q.[ApprovedByEmail]         = COALESCE(q.[ApprovedByEmail], vo.[ApprovedByEmail]),
        q.[SelectedSubcontractorId] = COALESCE(q.[SelectedSubcontractorId], vo.[SubcontractorId])
    FROM [VariationOrderQuotes] q
    INNER JOIN [VariationOrders] vo ON vo.[VariationOrderQuoteId] = q.[VariationOrderQuoteId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN

    UPDATE q SET q.[Status] = CASE
        WHEN q.[Status] IN (0, 1, 2, 3) THEN 0
        WHEN q.[Status] = 5 THEN 3
        WHEN q.[Status] = 4 AND EXISTS (
            SELECT 1 FROM [VariationOrders] vo
            WHERE vo.[VariationOrderQuoteId] = q.[VariationOrderQuoteId] AND vo.[Status] = 2) THEN 3
        ELSE 2
    END
    FROM [VariationOrderQuotes] q;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN

    UPDATE w SET w.[VariationOrderId] = vo.[VariationOrderQuoteId]
    FROM [WorkOrders] w
    INNER JOIN [VariationOrders] vo ON w.[VariationOrderId] = vo.[VariationOrderId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN

    UPDATE a SET a.[RecordId] = vo.[VariationOrderQuoteId]
    FROM [AuditEvents] a
    INNER JOIN [VariationOrders] vo ON a.[RecordId] = vo.[VariationOrderId]
    WHERE a.[RecordType] = 6;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    DROP TABLE [VariationOrders];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723120000_UnifyVariationOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723120000_UnifyVariationOrders', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_DirectoryUserRoles_DirectoryUserEmail'
                     AND object_id = OBJECT_ID(N'dbo.DirectoryUserRoles'))
    BEGIN
        CREATE INDEX IX_DirectoryUserRoles_DirectoryUserEmail ON dbo.DirectoryUserRoles (DirectoryUserEmail) INCLUDE (Role);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Requests_ProjectId_Status'
                     AND object_id = OBJECT_ID(N'dbo.Requests'))
    BEGIN
        CREATE INDEX IX_Requests_ProjectId_Status ON dbo.Requests (ProjectId, Status);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Requests_Kind_Status'
                     AND object_id = OBJECT_ID(N'dbo.Requests'))
    BEGIN
        CREATE INDEX IX_Requests_Kind_Status ON dbo.Requests (Kind, Status);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_RequestMessages_RequestId'
                     AND object_id = OBJECT_ID(N'dbo.RequestMessages'))
    BEGIN
        CREATE INDEX IX_RequestMessages_RequestId ON dbo.RequestMessages (RequestId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_VariationOrderQuotes_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.VariationOrderQuotes'))
    BEGIN
        CREATE INDEX IX_VariationOrderQuotes_ProjectId ON dbo.VariationOrderQuotes (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_VariationOrderQuotes_RequestId'
                     AND object_id = OBJECT_ID(N'dbo.VariationOrderQuotes'))
    BEGIN
        CREATE INDEX IX_VariationOrderQuotes_RequestId ON dbo.VariationOrderQuotes (RequestId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_WorkOrders_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.WorkOrders'))
    BEGIN
        CREATE INDEX IX_WorkOrders_ProjectId ON dbo.WorkOrders (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_WorkOrders_VariationOrderId'
                     AND object_id = OBJECT_ID(N'dbo.WorkOrders'))
    BEGIN
        CREATE INDEX IX_WorkOrders_VariationOrderId ON dbo.WorkOrders (VariationOrderId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_BidPackages_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.BidPackages'))
    BEGIN
        CREATE INDEX IX_BidPackages_ProjectId ON dbo.BidPackages (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_BidPackages_VariationOrderQuoteId'
                     AND object_id = OBJECT_ID(N'dbo.BidPackages'))
    BEGIN
        CREATE INDEX IX_BidPackages_VariationOrderQuoteId ON dbo.BidPackages (VariationOrderQuoteId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Quotes_BidPackageId'
                     AND object_id = OBJECT_ID(N'dbo.Quotes'))
    BEGIN
        CREATE INDEX IX_Quotes_BidPackageId ON dbo.Quotes (BidPackageId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_WorkOrderLines_WorkOrderId'
                     AND object_id = OBJECT_ID(N'dbo.WorkOrderLines'))
    BEGIN
        CREATE INDEX IX_WorkOrderLines_WorkOrderId ON dbo.WorkOrderLines (WorkOrderId) INCLUDE (CostCode, LineTotal);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Timesheets_ProjectId_Status'
                     AND object_id = OBJECT_ID(N'dbo.Timesheets'))
    BEGIN
        CREATE INDEX IX_Timesheets_ProjectId_Status ON dbo.Timesheets (ProjectId, Status) INCLUDE (CostCode, CostAmount, WorkerId, Hours);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_XeroLineTimesheetCovers_XeroLedgerLineId'
                     AND object_id = OBJECT_ID(N'dbo.XeroLineTimesheetCovers'))
    BEGIN
        CREATE INDEX IX_XeroLineTimesheetCovers_XeroLedgerLineId ON dbo.XeroLineTimesheetCovers (XeroLedgerLineId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_SiteAttendances_ProjectId_WorkDate'
                     AND object_id = OBJECT_ID(N'dbo.SiteAttendances'))
    BEGIN
        CREATE INDEX IX_SiteAttendances_ProjectId_WorkDate ON dbo.SiteAttendances (ProjectId, WorkDate);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Drawings_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.Drawings'))
    BEGIN
        CREATE INDEX IX_Drawings_ProjectId ON dbo.Drawings (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_DrawingRevisions_DrawingId'
                     AND object_id = OBJECT_ID(N'dbo.DrawingRevisions'))
    BEGIN
        CREATE INDEX IX_DrawingRevisions_DrawingId ON dbo.DrawingRevisions (DrawingId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_HsRecords_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.HsRecords'))
    BEGIN
        CREATE INDEX IX_HsRecords_ProjectId ON dbo.HsRecords (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_TodoItems_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.TodoItems'))
    BEGIN
        CREATE INDEX IX_TodoItems_ProjectId ON dbo.TodoItems (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Defects_ProjectId'
                     AND object_id = OBJECT_ID(N'dbo.Defects'))
    BEGIN
        CREATE INDEX IX_Defects_ProjectId ON dbo.Defects (ProjectId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_ComplianceDocuments_SubcontractorId'
                     AND object_id = OBJECT_ID(N'dbo.ComplianceDocuments'))
    BEGIN
        CREATE INDEX IX_ComplianceDocuments_SubcontractorId ON dbo.ComplianceDocuments (SubcontractorId);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725100000_AddPerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725100000_AddPerformanceIndexes', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[ArchitectInstructions]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ArchitectInstructions] (
            [ArchitectInstructionId] nvarchar(64)   NOT NULL,
            [ProjectId]              nvarchar(64)   NOT NULL,
            [Number]                 int            NOT NULL,
            [Reference]              nvarchar(64)   NOT NULL,
            [InstructionRef]         nvarchar(128)  NOT NULL,
            [Title]                  nvarchar(256)  NOT NULL,
            [Notes]                  nvarchar(2048) NULL,
            [InstructedAt]           datetimeoffset NULL,
            [ReceivedAt]             datetimeoffset NOT NULL,
            [IssuedByEmail]          nvarchar(256)  NOT NULL,
            [FiledByEmail]           nvarchar(256)  NOT NULL,
            [Source]                 int            NOT NULL,
            [FileName]               nvarchar(256)  NULL,
            [ContentType]            nvarchar(128)  NULL,
            [FileSizeBytes]          bigint         NULL,
            [BlobRef]                nvarchar(1024) NULL,
            CONSTRAINT [PK_ArchitectInstructions] PRIMARY KEY ([ArchitectInstructionId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[ArchitectInstructionVariations]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ArchitectInstructionVariations] (
            [ArchitectInstructionVariationId] nvarchar(64)   NOT NULL,
            [ArchitectInstructionId]          nvarchar(64)   NOT NULL,
            [VariationOrderId]                nvarchar(64)   NOT NULL,
            [LinkedAt]                        datetimeoffset NOT NULL,
            [LinkedByEmail]                   nvarchar(256)  NOT NULL,
            CONSTRAINT [PK_ArchitectInstructionVariations] PRIMARY KEY ([ArchitectInstructionVariationId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArchitectInstructions_ProjectId'
                   AND object_id = OBJECT_ID(N'[dbo].[ArchitectInstructions]'))
        CREATE INDEX [IX_ArchitectInstructions_ProjectId] ON [dbo].[ArchitectInstructions] ([ProjectId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArchitectInstructionVariations_ArchitectInstructionId'
                   AND object_id = OBJECT_ID(N'[dbo].[ArchitectInstructionVariations]'))
        CREATE INDEX [IX_ArchitectInstructionVariations_ArchitectInstructionId]
            ON [dbo].[ArchitectInstructionVariations] ([ArchitectInstructionId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArchitectInstructionVariations_VariationOrderId'
                   AND object_id = OBJECT_ID(N'[dbo].[ArchitectInstructionVariations]'))
        CREATE INDEX [IX_ArchitectInstructionVariations_VariationOrderId]
            ON [dbo].[ArchitectInstructionVariations] ([VariationOrderId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[RequestAttachments]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[RequestAttachments] (
            [RequestAttachmentId] nvarchar(64)   NOT NULL,
            [RequestId]           nvarchar(64)   NOT NULL,
            [ProjectId]           nvarchar(64)   NOT NULL,
            [Kind]                int            NOT NULL,
            [DrawingId]           nvarchar(64)   NULL,
            [DrawingRevisionId]   nvarchar(64)   NULL,
            [DrawingCode]         nvarchar(64)   NULL,
            [RevisionLabel]       nvarchar(16)   NULL,
            [FileName]            nvarchar(256)  NULL,
            [ContentType]         nvarchar(128)  NULL,
            [FileSizeBytes]       bigint         NULL,
            [BlobRef]             nvarchar(1024) NULL,
            [Caption]             nvarchar(512)  NULL,
            [AddedAt]             datetimeoffset NOT NULL,
            [AddedByEmail]        nvarchar(256)  NOT NULL,
            CONSTRAINT [PK_RequestAttachments] PRIMARY KEY ([RequestAttachmentId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RequestAttachments_RequestId'
                   AND object_id = OBJECT_ID(N'[dbo].[RequestAttachments]'))
        CREATE INDEX [IX_RequestAttachments_RequestId] ON [dbo].[RequestAttachments] ([RequestId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEvents_RecordId'
                   AND object_id = OBJECT_ID(N'[dbo].[AuditEvents]'))
        CREATE INDEX [IX_AuditEvents_RecordId] ON [dbo].[AuditEvents] ([RecordId]) INCLUDE ([OccurredAt]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726100000_AddArchitectInstructionsAndRequestAttachments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726100000_AddArchitectInstructionsAndRequestAttachments', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[ProjectContracts]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ProjectContracts] (
            [ProjectContractId]                nvarchar(64)   NOT NULL,
            [ProjectId]                        nvarchar(64)   NOT NULL,

            [Form]                             int            NOT NULL,
            [FormEdition]                      nvarchar(16)   NULL,
            [BespokeDeviations]                nvarchar(4000) NULL,

            [EmployerName]                     nvarchar(256)  NULL,
            [ContractAdministratorName]        nvarchar(256)  NULL,
            [ContractAdministratorEmail]       nvarchar(256)  NULL,
            [ArchitectName]                    nvarchar(256)  NULL,
            [ArchitectEmail]                   nvarchar(256)  NULL,
            [ContractorName]                   nvarchar(256)  NULL,

            [ContractSum]                      decimal(18,4)  NOT NULL,
            [LiquidatedDamagesPerWeek]         decimal(18,4)  NOT NULL,

            [ContractDate]                     datetimeoffset NULL,
            [PossessionDate]                   datetimeoffset NULL,
            [CompletionDate]                   datetimeoffset NULL,

            [RetentionPercent]                 decimal(18,4)  NOT NULL,
            [RetentionPercentAfterCompletion]  decimal(18,4)  NOT NULL,
            [DefectsLiabilityPeriodMonths]     int            NOT NULL,

            [ApplicationCutOffDayOfMonth]      int            NULL,
            [PaymentNoticeDays]                int            NOT NULL,
            [PayLessNoticeDays]                int            NOT NULL,
            [FinalDateForPaymentDays]          int            NOT NULL,

            [OhpDirectWorksPercent]            decimal(18,4)  NOT NULL,
            [OhpSubcontractorPercent]          decimal(18,4)  NOT NULL,
            [AttendanceOnClientDirectPercent]  decimal(18,4)  NOT NULL,
            [DayworkLabourPercent]             decimal(18,4)  NOT NULL,
            [DayworkMaterialsPercent]          decimal(18,4)  NOT NULL,
            [DayworkPlantPercent]              decimal(18,4)  NOT NULL,

            [DocumentBlobRef]                  nvarchar(1024) NULL,
            [DocumentFileName]                 nvarchar(256)  NULL,
            [DocumentContentType]              nvarchar(128)  NULL,
            [DocumentFileSizeBytes]            bigint         NULL,
            [DocumentUploadedAt]               datetimeoffset NULL,
            [DocumentUploadedByEmail]          nvarchar(256)  NULL,

            [UpdatedByEmail]                   nvarchar(256)  NULL,
            [UpdatedAt]                        datetimeoffset NOT NULL,
            CONSTRAINT [PK_ProjectContracts] PRIMARY KEY ([ProjectContractId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectContracts_ProjectId'
                   AND object_id = OBJECT_ID(N'[dbo].[ProjectContracts]'))
        CREATE UNIQUE INDEX [IX_ProjectContracts_ProjectId] ON [dbo].[ProjectContracts] ([ProjectId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[AiConversations]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[AiConversations] (
            [ConversationId]  nvarchar(64)   NOT NULL,
            [ProjectId]       nvarchar(64)   NULL,
            [Route]           nvarchar(512)  NULL,
            [CapabilityKey]   nvarchar(64)   NOT NULL,
            [StartedByEmail]  nvarchar(256)  NOT NULL,
            [Title]           nvarchar(256)  NULL,
            [StartedAt]       datetimeoffset NOT NULL,
            [LastMessageAt]   datetimeoffset NOT NULL,
            CONSTRAINT [PK_AiConversations] PRIMARY KEY ([ConversationId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[AiConversationMessages]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[AiConversationMessages] (
            [MessageId]       nvarchar(64)   NOT NULL,
            [ConversationId]  nvarchar(64)   NOT NULL,
            [Role]            int            NOT NULL,
            [Body]            nvarchar(max)  NOT NULL,
            [ToolName]        nvarchar(128)  NULL,
            [ToolUseId]       nvarchar(128)  NULL,
            [Sequence]        int            NOT NULL,
            [PostedAt]        datetimeoffset NOT NULL,
            CONSTRAINT [PK_AiConversationMessages] PRIMARY KEY ([MessageId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AiConversationMessages_ConversationId_Sequence'
                   AND object_id = OBJECT_ID(N'[dbo].[AiConversationMessages]'))
        CREATE INDEX [IX_AiConversationMessages_ConversationId_Sequence]
            ON [dbo].[AiConversationMessages] ([ConversationId], [Sequence]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AiConversations_StartedByEmail_LastMessageAt'
                   AND object_id = OBJECT_ID(N'[dbo].[AiConversations]'))
        CREATE INDEX [IX_AiConversations_StartedByEmail_LastMessageAt]
            ON [dbo].[AiConversations] ([StartedByEmail], [LastMessageAt]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726210000_AddProjectContractsAndAiConversations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726210000_AddProjectContractsAndAiConversations', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726220000_AddAgentActivity'
)
BEGIN

    IF OBJECT_ID(N'[dbo].[AgentActivity]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[AgentActivity] (
            [ActivityId]       nvarchar(64)   NOT NULL,
            [AgentKey]         nvarchar(64)   NOT NULL,
            [Trigger]          int            NOT NULL,

            [ActorEmail]       nvarchar(256)  NOT NULL,
            [IsAutonomous]     bit            NOT NULL,

            [Action]           nvarchar(128)  NOT NULL,
            [Outcome]          int            NOT NULL,
            [Summary]          nvarchar(1024) NOT NULL,

            [ConversationId]   nvarchar(64)   NULL,
            [ProjectId]        nvarchar(64)   NULL,
            [RecordReference]  nvarchar(64)   NULL,
            [Route]            nvarchar(512)  NULL,

            [ToolsUsed]        nvarchar(512)  NULL,

            [DurationMs]       int            NOT NULL,
            [InputTokens]      int            NOT NULL,
            [OutputTokens]     int            NOT NULL,
            [CostPence]        decimal(18,4)  NOT NULL,

            [OccurredAt]       datetimeoffset NOT NULL,
            CONSTRAINT [PK_AgentActivity] PRIMARY KEY ([ActivityId])
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726220000_AddAgentActivity'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentActivity_OccurredAt'
                   AND object_id = OBJECT_ID(N'[dbo].[AgentActivity]'))
        CREATE INDEX [IX_AgentActivity_OccurredAt] ON [dbo].[AgentActivity] ([OccurredAt] DESC);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726220000_AddAgentActivity'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentActivity_IsAutonomous_OccurredAt'
                   AND object_id = OBJECT_ID(N'[dbo].[AgentActivity]'))
        CREATE INDEX [IX_AgentActivity_IsAutonomous_OccurredAt]
            ON [dbo].[AgentActivity] ([IsAutonomous], [OccurredAt] DESC);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726220000_AddAgentActivity'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentActivity_ProjectId'
                   AND object_id = OBJECT_ID(N'[dbo].[AgentActivity]'))
        CREATE INDEX [IX_AgentActivity_ProjectId] ON [dbo].[AgentActivity] ([ProjectId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726220000_AddAgentActivity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726220000_AddAgentActivity', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726230000_AddAiToolCallsJson'
)
BEGIN

    IF COL_LENGTH(N'[dbo].[AiConversationMessages]', N'ToolCallsJson') IS NULL
        ALTER TABLE [dbo].[AiConversationMessages] ADD [ToolCallsJson] nvarchar(max) NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726230000_AddAiToolCallsJson'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726230000_AddAiToolCallsJson', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727100000_AddAiConversationRecordScope'
)
BEGIN

    IF COL_LENGTH(N'[dbo].[AiConversations]', N'ScopeRecordType') IS NULL
        ALTER TABLE [dbo].[AiConversations] ADD [ScopeRecordType] nvarchar(64) NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727100000_AddAiConversationRecordScope'
)
BEGIN

    IF COL_LENGTH(N'[dbo].[AiConversations]', N'ScopeRecordId') IS NULL
        ALTER TABLE [dbo].[AiConversations] ADD [ScopeRecordId] nvarchar(64) NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727100000_AddAiConversationRecordScope'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AiConversations_ScopeRecordId'
                   AND object_id = OBJECT_ID(N'[dbo].[AiConversations]'))
        CREATE INDEX [IX_AiConversations_ScopeRecordId]
            ON [dbo].[AiConversations] ([ScopeRecordId], [LastMessageAt]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727100000_AddAiConversationRecordScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727100000_AddAiConversationRecordScope', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727120000_AddXeroLinePaymentState'
)
BEGIN

    IF COL_LENGTH(N'[dbo].[XeroLedgerLines]', N'InvoiceTotal') IS NULL
    BEGIN
        ALTER TABLE [dbo].[XeroLedgerLines]
            ADD [InvoiceTotal] decimal(18,4) NOT NULL
                CONSTRAINT [DF_XeroLedgerLines_InvoiceTotal] DEFAULT (0);
    END;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727120000_AddXeroLinePaymentState'
)
BEGIN

    IF COL_LENGTH(N'[dbo].[XeroLedgerLines]', N'AmountDue') IS NULL
    BEGIN
        ALTER TABLE [dbo].[XeroLedgerLines]
            ADD [AmountDue] decimal(18,4) NOT NULL
                CONSTRAINT [DF_XeroLedgerLines_AmountDue] DEFAULT (0);
    END;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727120000_AddXeroLinePaymentState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727120000_AddXeroLinePaymentState', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728100000_AddWorkOrderDepositAndSubcontractorPaymentTerms'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [DepositRequired] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728100000_AddWorkOrderDepositAndSubcontractorPaymentTerms'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [DepositPercent] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728100000_AddWorkOrderDepositAndSubcontractorPaymentTerms'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [PaymentTermsDays] int NOT NULL DEFAULT 30;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728100000_AddWorkOrderDepositAndSubcontractorPaymentTerms'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728100000_AddWorkOrderDepositAndSubcontractorPaymentTerms', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728120000_AddSubcontractorAddress'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [AddressLine] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728120000_AddSubcontractorAddress'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [Postcode] nvarchar(32) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728120000_AddSubcontractorAddress'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728120000_AddSubcontractorAddress', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728150000_AddXeroSupplierLinksAndCompanyContacts'
)
BEGIN
    CREATE TABLE [SubcontractorXeroLinks] (
        [SubcontractorXeroLinkId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [XeroContactId] nvarchar(64) NOT NULL,
        [XeroContactName] nvarchar(256) NOT NULL,
        [ImportedAt] datetimeoffset NOT NULL,
        [ImportedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_SubcontractorXeroLinks] PRIMARY KEY ([SubcontractorXeroLinkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728150000_AddXeroSupplierLinksAndCompanyContacts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SubcontractorXeroLinks_XeroContactId] ON [SubcontractorXeroLinks] ([XeroContactId]) WHERE [XeroContactId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728150000_AddXeroSupplierLinksAndCompanyContacts'
)
BEGIN
    CREATE INDEX [IX_SubcontractorXeroLinks_SubcontractorId] ON [SubcontractorXeroLinks] ([SubcontractorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728150000_AddXeroSupplierLinksAndCompanyContacts'
)
BEGIN
    CREATE TABLE [CompanyContacts] (
        [CompanyContactId] nvarchar(64) NOT NULL,
        [SubcontractorId] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Purpose] nvarchar(128) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Phone] nvarchar(64) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_CompanyContacts] PRIMARY KEY ([CompanyContactId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728150000_AddXeroSupplierLinksAndCompanyContacts'
)
BEGIN
    CREATE INDEX [IX_CompanyContacts_SubcontractorId] ON [CompanyContacts] ([SubcontractorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728150000_AddXeroSupplierLinksAndCompanyContacts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728150000_AddXeroSupplierLinksAndCompanyContacts', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729100000_TodoItemsAssigneePerson'
)
BEGIN
    ALTER TABLE [TodoItems] ADD [AssigneePersonEmail] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729100000_TodoItemsAssigneePerson'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729100000_TodoItemsAssigneePerson', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730100000_AddDirectoryUserRevocation'
)
BEGIN
    ALTER TABLE [DirectoryUsers] ADD [RevokedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730100000_AddDirectoryUserRevocation'
)
BEGIN
    ALTER TABLE [DirectoryUsers] ADD [RevokedBy] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730100000_AddDirectoryUserRevocation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730100000_AddDirectoryUserRevocation', N'8.0.10');
END;
GO

COMMIT;
GO

