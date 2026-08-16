BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814190000_AddXeroDisputeMessages'
)
BEGIN
    CREATE TABLE [XeroDisputeMessages] (
        [XeroDisputeMessageId] nvarchar(64) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [Author] nvarchar(256) NOT NULL,
        [Body] nvarchar(2048) NOT NULL,
        [SentAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_XeroDisputeMessages] PRIMARY KEY ([XeroDisputeMessageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814190000_AddXeroDisputeMessages'
)
BEGIN
    CREATE INDEX [IX_XeroDisputeMessages_XeroLedgerLineId] ON [XeroDisputeMessages] ([XeroLedgerLineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814190000_AddXeroDisputeMessages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814190000_AddXeroDisputeMessages', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP TABLE [IntakeEmails];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP TABLE [MailboxSyncStates];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_XeroLedgerLines_AllocationStatus] ON [XeroLedgerLines];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_XeroLedgerLines_ProjectId_CostCenterCode] ON [XeroLedgerLines];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_XeroLedgerLines_XeroInvoiceId] ON [XeroLedgerLines];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_XeroCostSplits_ProjectId_CostCenterCode] ON [XeroCostSplits];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_XeroCostSplits_XeroLedgerLineId] ON [XeroCostSplits];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ValuationReportSnapshots_ProjectId] ON [ValuationReportSnapshots];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ValuationReportSnapshots_ValuationInvoiceId] ON [ValuationReportSnapshots];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ValuationReportSnapshotLines_ValuationReportSnapshotId] ON [ValuationReportSnapshotLines];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ValuationLineItems_ProjectId] ON [ValuationLineItems];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ValuationInvoiceEvents_ValuationInvoiceId] ON [ValuationInvoiceEvents];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ValuationClaims_ProjectId] ON [ValuationClaims];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_Trades_Name] ON [Trades];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_SubcontractorTrades_SubcontractorId_TradeId] ON [SubcontractorTrades];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_RequestItems_RequestId] ON [RequestItems];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_RequestAgents_RequestId] ON [RequestAgents];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ProjectContacts_ProjectId] ON [ProjectContacts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_ClaimLines_ValuationClaimId] ON [ClaimLines];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_AgentProposals_RequestId] ON [AgentProposals];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DROP INDEX [IX_AgentChatMessages_RequestId_AgentKey] ON [AgentChatMessages];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    EXEC sp_rename N'[Quotes].[PaidAt]', N'ReceivedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    EXEC sp_rename N'[InfoChaseItems].[RaisedAt]', N'RequestedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    EXEC sp_rename N'[DrawingRevisions].[PaidAt]', N'ReceivedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    EXEC sp_rename N'[AccessRequests].[RaisedAt]', N'RequestedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [AmountDue] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [InvoiceTotal] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [AcceptedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [AcceptedByEmail] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [AcceptedByName] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [DepositPercent] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [DepositRequired] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [ProgrammeNotes] nvarchar(2000) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [WorkOrders] ADD [ProgrammeStart] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [CommercialBasis] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [Exclusions] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [ProgrammeImpact] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationReportSnapshots] ADD [DepositPercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationReportSnapshots] ADD [DepositReleased] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationReportSnapshots] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [DepositCredited] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationClaims] ADD [DepositPercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationClaims] ADD [DepositReleased] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ValuationClaims] ADD [DepositReleasedOpening] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [AddressLine] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [PaymentTermsDays] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [Postcode] nvarchar(32) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteLineItems]') AND [c].[name] = N'Total');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [QuoteLineItems] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [QuoteLineItems] ALTER COLUMN [Total] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteLineItems]') AND [c].[name] = N'Rate');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [QuoteLineItems] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [QuoteLineItems] ALTER COLUMN [Rate] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteLineItems]') AND [c].[name] = N'Quantity');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [QuoteLineItems] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [QuoteLineItems] ALTER COLUMN [Quantity] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Projects]') AND [c].[name] = N'ExpectedMonthlyValuation');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Projects] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Projects] ALTER COLUMN [ExpectedMonthlyValuation] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ProjectContacts] ADD [PartyContactId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [ProjectContacts] ADD [Routing] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [InviteDraftBcc] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [InviteDraftBody] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [InviteDraftCc] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [InviteDraftSavedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [InviteDraftSubject] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [InviteDraftTo] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [MaterialsApplicable] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BidPackageLineItems]') AND [c].[name] = N'Quantity');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [BidPackageLineItems] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [BidPackageLineItems] ALTER COLUMN [Quantity] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [BoqLineItemId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [CostCode] nvarchar(32) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [Coverage] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    ALTER TABLE [BidPackageLineItems] ADD [VariationOrderQuoteId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [AgentActivity] (
        [ActivityId] nvarchar(64) NOT NULL,
        [AgentKey] nvarchar(64) NOT NULL,
        [Trigger] int NOT NULL,
        [ActorEmail] nvarchar(256) NOT NULL,
        [IsAutonomous] bit NOT NULL,
        [Action] nvarchar(128) NOT NULL,
        [Outcome] int NOT NULL,
        [Summary] nvarchar(1024) NOT NULL,
        [ConversationId] nvarchar(64) NULL,
        [ProjectId] nvarchar(64) NULL,
        [RecordReference] nvarchar(64) NULL,
        [Route] nvarchar(512) NULL,
        [ToolsUsed] nvarchar(512) NULL,
        [DurationMs] int NOT NULL,
        [InputTokens] int NOT NULL,
        [OutputTokens] int NOT NULL,
        [CostPence] decimal(18,4) NOT NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AgentActivity] PRIMARY KEY ([ActivityId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [AiConversationMessages] (
        [MessageId] nvarchar(64) NOT NULL,
        [ConversationId] nvarchar(64) NOT NULL,
        [Role] int NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [ToolName] nvarchar(128) NULL,
        [ToolUseId] nvarchar(128) NULL,
        [ToolCallsJson] nvarchar(max) NULL,
        [Sequence] int NOT NULL,
        [PostedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiConversationMessages] PRIMARY KEY ([MessageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [AiConversations] (
        [ConversationId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NULL,
        [Route] nvarchar(512) NULL,
        [CapabilityKey] nvarchar(64) NOT NULL,
        [ScopeRecordType] nvarchar(64) NULL,
        [ScopeRecordId] nvarchar(64) NULL,
        [StartedByEmail] nvarchar(256) NOT NULL,
        [Title] nvarchar(256) NULL,
        [StartedAt] datetimeoffset NOT NULL,
        [LastMessageAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiConversations] PRIMARY KEY ([ConversationId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ArchitectInstructions] (
        [ArchitectInstructionId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Number] int NOT NULL,
        [Reference] nvarchar(64) NOT NULL,
        [InstructionRef] nvarchar(128) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Notes] nvarchar(2048) NULL,
        [InstructedAt] datetimeoffset NULL,
        [ReceivedAt] datetimeoffset NOT NULL,
        [IssuedByEmail] nvarchar(256) NOT NULL,
        [FiledByEmail] nvarchar(256) NOT NULL,
        [Source] int NOT NULL,
        [FileName] nvarchar(256) NULL,
        [ContentType] nvarchar(128) NULL,
        [FileSizeBytes] bigint NULL,
        [BlobRef] nvarchar(1024) NULL,
        CONSTRAINT [PK_ArchitectInstructions] PRIMARY KEY ([ArchitectInstructionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ArchitectInstructionVariations] (
        [ArchitectInstructionVariationId] nvarchar(64) NOT NULL,
        [ArchitectInstructionId] nvarchar(64) NOT NULL,
        [VariationOrderId] nvarchar(64) NOT NULL,
        [LinkedAt] datetimeoffset NOT NULL,
        [LinkedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_ArchitectInstructionVariations] PRIMARY KEY ([ArchitectInstructionVariationId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [CostCentreCostProgress] (
        [CostCentreCostProgressId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        [CostCompletionPercent] decimal(18,4) NOT NULL,
        [IsFinalised] bit NOT NULL,
        CONSTRAINT [PK_CostCentreCostProgress] PRIMARY KEY ([CostCentreCostProgressId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ProgressUpdates] (
        [ProgressUpdateId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [WorkDate] datetimeoffset NULL,
        [WeatherSummary] nvarchar(256) NOT NULL,
        [WeatherObservedAt] datetimeoffset NULL,
        [WeatherTempHighC] int NULL,
        [WeatherTempLowC] int NULL,
        [WeatherWindMph] int NULL,
        [WeatherHumidityPercent] int NULL,
        [WeatherPrecipInches] decimal(18,4) NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgressUpdates] PRIMARY KEY ([ProgressUpdateId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ProjectContracts] (
        [ProjectContractId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Form] int NOT NULL,
        [FormEdition] nvarchar(16) NULL,
        [BespokeDeviations] nvarchar(4000) NULL,
        [EmployerName] nvarchar(256) NULL,
        [ContractAdministratorName] nvarchar(256) NULL,
        [ContractAdministratorEmail] nvarchar(256) NULL,
        [ArchitectName] nvarchar(256) NULL,
        [ArchitectEmail] nvarchar(256) NULL,
        [ContractorName] nvarchar(256) NULL,
        [ContractSum] decimal(18,4) NOT NULL,
        [LiquidatedDamagesPerWeek] decimal(18,4) NOT NULL,
        [ContractDate] datetimeoffset NULL,
        [PossessionDate] datetimeoffset NULL,
        [CompletionDate] datetimeoffset NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [RetentionPercentAfterCompletion] decimal(18,4) NOT NULL,
        [DefectsLiabilityPeriodMonths] int NOT NULL,
        [ApplicationCutOffDayOfMonth] int NULL,
        [PaymentNoticeDays] int NOT NULL,
        [PayLessNoticeDays] int NOT NULL,
        [FinalDateForPaymentDays] int NOT NULL,
        [OhpDirectWorksPercent] decimal(18,4) NOT NULL,
        [OhpSubcontractorPercent] decimal(18,4) NOT NULL,
        [AttendanceOnClientDirectPercent] decimal(18,4) NOT NULL,
        [DayworkLabourPercent] decimal(18,4) NOT NULL,
        [DayworkMaterialsPercent] decimal(18,4) NOT NULL,
        [DayworkPlantPercent] decimal(18,4) NOT NULL,
        [DocumentBlobRef] nvarchar(1024) NULL,
        [DocumentFileName] nvarchar(256) NULL,
        [DocumentContentType] nvarchar(128) NULL,
        [DocumentFileSizeBytes] bigint NULL,
        [DocumentUploadedAt] datetimeoffset NULL,
        [DocumentUploadedByEmail] nvarchar(256) NULL,
        [UpdatedByEmail] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProjectContracts] PRIMARY KEY ([ProjectContractId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ProjectRetentions] (
        [ProjectRetentionId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [RetentionPercent] decimal(18,4) NOT NULL,
        [CompletionReleasePercent] decimal(18,4) NOT NULL,
        [DepositPercent] decimal(18,4) NOT NULL,
        [DepositReleasedOpening] decimal(18,4) NOT NULL,
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ReconciliationPackageCostLines] (
        [ReconciliationPackageCostLineId] nvarchar(64) NOT NULL,
        [ReconciliationPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ReconciliationPackageCostLines] PRIMARY KEY ([ReconciliationPackageCostLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [ReconciliationPackages] (
        [ReconciliationPackageId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [IsLocked] bit NOT NULL,
        [LockedAt] datetimeoffset NULL,
        [LockedSalesValue] decimal(18,4) NOT NULL,
        [LockedClaimedToDate] decimal(18,4) NOT NULL,
        [LockedTargetCost] decimal(18,4) NOT NULL,
        [LockedWoCommitted] decimal(18,4) NOT NULL,
        [LockedInvoicedCost] decimal(18,4) NOT NULL,
        [LockedProfitLoss] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ReconciliationPackages] PRIMARY KEY ([ReconciliationPackageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [RequestAttachments] (
        [RequestAttachmentId] nvarchar(64) NOT NULL,
        [RequestId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Kind] int NOT NULL,
        [DrawingId] nvarchar(64) NULL,
        [DrawingRevisionId] nvarchar(64) NULL,
        [DrawingCode] nvarchar(64) NULL,
        [RevisionLabel] nvarchar(16) NULL,
        [FileName] nvarchar(256) NULL,
        [ContentType] nvarchar(128) NULL,
        [FileSizeBytes] bigint NULL,
        [BlobRef] nvarchar(1024) NULL,
        [Caption] nvarchar(512) NULL,
        [AddedAt] datetimeoffset NOT NULL,
        [AddedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_RequestAttachments] PRIMARY KEY ([RequestAttachmentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [SkillReferences] (
        [SkillReferenceId] nvarchar(64) NOT NULL,
        [SkillKey] nvarchar(128) NOT NULL,
        [RefKey] nvarchar(128) NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [UpdatedByEmail] nvarchar(256) NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_SkillReferences] PRIMARY KEY ([SkillReferenceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [SkillRevisions] (
        [SkillRevisionId] nvarchar(64) NOT NULL,
        [SkillKey] nvarchar(128) NOT NULL,
        [Version] int NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [SavedByEmail] nvarchar(256) NOT NULL,
        [SavedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_SkillRevisions] PRIMARY KEY ([SkillRevisionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [Skills] (
        [SkillKey] nvarchar(128) NOT NULL,
        [AgentKey] nvarchar(64) NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Pinned] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Version] int NOT NULL,
        [UpdatedByEmail] nvarchar(256) NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Skills] PRIMARY KEY ([SkillKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [TodoItemLinks] (
        [TodoItemLinkId] nvarchar(64) NOT NULL,
        [TodoItemAId] nvarchar(64) NOT NULL,
        [TodoItemBId] nvarchar(64) NOT NULL,
        [LinkedAt] datetimeoffset NOT NULL,
        [LinkedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_TodoItemLinks] PRIMARY KEY ([TodoItemLinkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [UsefulInformationNotes] (
        [UsefulInformationNoteId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedByEmail] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_UsefulInformationNotes] PRIMARY KEY ([UsefulInformationNoteId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE TABLE [XeroDisputeMessages] (
        [XeroDisputeMessageId] nvarchar(64) NOT NULL,
        [XeroLedgerLineId] nvarchar(140) NOT NULL,
        [Author] nvarchar(256) NOT NULL,
        [Body] nvarchar(2048) NOT NULL,
        [SentAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_XeroDisputeMessages] PRIMARY KEY ([XeroDisputeMessageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
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
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AgentActivity_IsAutonomous_OccurredAt] ON [AgentActivity] ([IsAutonomous], [OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AgentActivity_OccurredAt] ON [AgentActivity] ([OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AgentActivity_ProjectId] ON [AgentActivity] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AiConversationMessages_ConversationId_Sequence] ON [AiConversationMessages] ([ConversationId], [Sequence]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AiConversations_ScopeRecordId] ON [AiConversations] ([ScopeRecordId], [LastMessageAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AiConversations_StartedByEmail_LastMessageAt] ON [AiConversations] ([StartedByEmail], [LastMessageAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_ArchitectInstructions_ProjectId] ON [ArchitectInstructions] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_ArchitectInstructionVariations_ArchitectInstructionId] ON [ArchitectInstructionVariations] ([ArchitectInstructionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_ArchitectInstructionVariations_VariationOrderId] ON [ArchitectInstructionVariations] ([VariationOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_RecordId] ON [AuditEvents] ([RecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_CompanyContacts_SubcontractorId] ON [CompanyContacts] ([SubcontractorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectContracts_ProjectId] ON [ProjectContracts] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_RequestAttachments_RequestId] ON [RequestAttachments] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SkillReferences_SkillKey_RefKey] ON [SkillReferences] ([SkillKey], [RefKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_SkillRevisions_SkillKey_Version] ON [SkillRevisions] ([SkillKey], [Version]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_Skills_AgentKey_IsActive] ON [Skills] ([AgentKey], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_SubcontractorXeroLinks_SubcontractorId] ON [SubcontractorXeroLinks] ([SubcontractorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubcontractorXeroLinks_XeroContactId] ON [SubcontractorXeroLinks] ([XeroContactId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TodoItemLinks_TodoItemAId_TodoItemBId] ON [TodoItemLinks] ([TodoItemAId], [TodoItemBId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_TodoItemLinks_TodoItemBId] ON [TodoItemLinks] ([TodoItemBId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_UsefulInformationNotes_ProjectId] ON [UsefulInformationNotes] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    CREATE INDEX [IX_XeroDisputeMessages_XeroLedgerLineId] ON [XeroDisputeMessages] ([XeroLedgerLineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260816113113_AddBidPackageInviteDraft', N'8.0.10');
END;
GO

COMMIT;
GO

