-- ============================================================================
-- WidenWrittenTextColumns  (2026-09-24)
-- ============================================================================
-- Nigel could not approve V33 on Abbot Road: line A10's description was 550
-- characters and ValuationLineItems.Description was nvarchar(512). The same
-- ceiling sat on every column people write sentences into, so 82 columns
-- across 63 tables go to nvarchar(max), and
-- ReconciliationPackages.Name goes from 128 to 256 to take a work order's title.
-- Widening only: no data changes; no index or constraint touches these columns.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260924160000_WidenWrittenTextColumns.cs.
-- Safe to apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i widen-written-text-columns.sql -b -o widen-written-text-columns.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924160000_WidenWrittenTextColumns')
BEGIN
    ALTER TABLE [ArchitectInstructions] ALTER COLUMN [Notes] nvarchar(max) NULL;
    ALTER TABLE [BidPackageLineItems] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [BidPackages] ALTER COLUMN [SpecificationSummary] nvarchar(max) NOT NULL;
    ALTER TABLE [BoqLineItems] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [ClaimLines] ALTER COLUMN [Comments] nvarchar(max) NOT NULL;
    ALTER TABLE [ClaimLines] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [CompanyRegisterItems] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [ContraCharges] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [Dayworks] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [Defects] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [DrawingIssueRecords] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [DrivingLicenceChecks] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [Eots] ALTER COLUMN [Reason] nvarchar(max) NOT NULL;
    ALTER TABLE [HsAuditItems] ALTER COLUMN [Findings] nvarchar(max) NOT NULL;
    ALTER TABLE [HsAudits] ALTER COLUMN [FurtherComments] nvarchar(max) NOT NULL;
    ALTER TABLE [HsRecords] ALTER COLUMN [Summary] nvarchar(max) NOT NULL;
    ALTER TABLE [ImagineRounds] ALTER COLUMN [Brief] nvarchar(max) NOT NULL;
    ALTER TABLE [InventoryItems] ALTER COLUMN [LocationDetails] nvarchar(max) NOT NULL;
    ALTER TABLE [InventoryItems] ALTER COLUMN [ProductDetails] nvarchar(max) NOT NULL;
    ALTER TABLE [KpiEmails] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [LabourChaseDismissals] ALTER COLUMN [Reason] nvarchar(max) NOT NULL;
    ALTER TABLE [LabourSettlementVariances] ALTER COLUMN [Reason] nvarchar(max) NOT NULL;
    ALTER TABLE [LadClaims] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [LeadActivities] ALTER COLUMN [Summary] nvarchar(max) NOT NULL;
    ALTER TABLE [LeadEstimateLines] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [LeadEstimates] ALTER COLUMN [Exclusions] nvarchar(max) NOT NULL;
    ALTER TABLE [LeadEstimates] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [LeadEstimates] ALTER COLUMN [Scope] nvarchar(max) NOT NULL;
    ALTER TABLE [Leads] ALTER COLUMN [LostReason] nvarchar(max) NULL;
    ALTER TABLE [Leads] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [Leads] ALTER COLUMN [Summary] nvarchar(max) NOT NULL;
    ALTER TABLE [MobilisationItems] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [PolicyDocuments] ALTER COLUMN [Summary] nvarchar(max) NOT NULL;
    ALTER TABLE [ProgrammeVariationEffects] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [ProgressReports] ALTER COLUMN [Introduction] nvarchar(max) NOT NULL;
    ALTER TABLE [ProgressReports] ALTER COLUMN [UpcomingWorks] nvarchar(max) NOT NULL;
    ALTER TABLE [ProgressReports] ALTER COLUMN [WorkCompleted] nvarchar(max) NOT NULL;
    ALTER TABLE [ProjectContractAmendments] ALTER COLUMN [Notes] nvarchar(max) NULL;
    ALTER TABLE [ProjectContracts] ALTER COLUMN [BespokeDeviations] nvarchar(max) NULL;
    ALTER TABLE [QsAccruals] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [QuoteLineItems] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [Quotes] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [RequestItems] ALTER COLUMN [Response] nvarchar(max) NULL;
    ALTER TABLE [RequestMessages] ALTER COLUMN [Body] nvarchar(max) NOT NULL;
    ALTER TABLE [Requests] ALTER COLUMN [ClientNotes] nvarchar(max) NULL;
    ALTER TABLE [Requests] ALTER COLUMN [InternalNotes] nvarchar(max) NULL;
    ALTER TABLE [Requests] ALTER COLUMN [RelatedDrawingSpec] nvarchar(max) NULL;
    ALTER TABLE [RightToWorkChecks] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [SalesProposals] ALTER COLUMN [DeclineReason] nvarchar(max) NULL;
    ALTER TABLE [SalesStrategies] ALTER COLUMN [Brief] nvarchar(max) NOT NULL;
    ALTER TABLE [SalesStrategies] ALTER COLUMN [Evidence] nvarchar(max) NOT NULL;
    ALTER TABLE [SalesStrategies] ALTER COLUMN [Hypothesis] nvarchar(max) NOT NULL;
    ALTER TABLE [SalesStrategies] ALTER COLUMN [Proposition] nvarchar(max) NOT NULL;
    ALTER TABLE [SiteInstructions] ALTER COLUMN [Instruction] nvarchar(max) NOT NULL;
    ALTER TABLE [SitePhotos] ALTER COLUMN [ArchiveNote] nvarchar(max) NOT NULL;
    ALTER TABLE [SiteReports] ALTER COLUMN [Narrative] nvarchar(max) NOT NULL;
    ALTER TABLE [SubcontractorVariationRequests] ALTER COLUMN [RejectionReason] nvarchar(max) NOT NULL;
    ALTER TABLE [TenderEnquiries] ALTER COLUMN [DecisionNote] nvarchar(max) NOT NULL;
    ALTER TABLE [TenderEnquiries] ALTER COLUMN [ScopeSummary] nvarchar(max) NOT NULL;
    ALTER TABLE [Timesheets] ALTER COLUMN [RejectionReason] nvarchar(max) NOT NULL;
    ALTER TABLE [TodoItems] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [UsefulInformationNotes] ALTER COLUMN [Body] nvarchar(max) NOT NULL;
    ALTER TABLE [ValuationInvoiceEvents] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [ValuationInvoices] ALTER COLUMN [RejectionReason] nvarchar(max) NULL;
    ALTER TABLE [ValuationLineItems] ALTER COLUMN [Comments] nvarchar(max) NOT NULL;
    ALTER TABLE [ValuationLineItems] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [VariationOrderMessages] ALTER COLUMN [Body] nvarchar(max) NOT NULL;
    ALTER TABLE [VariationOrderQuotes] ALTER COLUMN [CommercialBasis] nvarchar(max) NULL;
    ALTER TABLE [VariationOrderQuotes] ALTER COLUMN [Exclusions] nvarchar(max) NULL;
    ALTER TABLE [VariationOrderQuotes] ALTER COLUMN [ProgrammeImpact] nvarchar(max) NULL;
    ALTER TABLE [VatAnalyses] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [WalkRoundNotes] ALTER COLUMN [Notes] nvarchar(max) NOT NULL;
    ALTER TABLE [WeeklyCashflowItems] ALTER COLUMN [Notes] nvarchar(max) NULL;
    ALTER TABLE [WorkOrderLines] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [WorkOrders] ALTER COLUMN [ProgrammeNotes] nvarchar(max) NOT NULL;
    ALTER TABLE [WorkOrders] ALTER COLUMN [Scope] nvarchar(max) NOT NULL;
    ALTER TABLE [WorkerAbsences] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [WorkerSettlementLines] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [WorkstationActions] ALTER COLUMN [Note] nvarchar(max) NOT NULL;
    ALTER TABLE [XeroDisputeMessages] ALTER COLUMN [Body] nvarchar(max) NOT NULL;
    ALTER TABLE [ReconciliationPackages] ALTER COLUMN [Name] nvarchar(256) NOT NULL;
    ALTER TABLE [XeroLedgerLines] ALTER COLUMN [Description] nvarchar(max) NULL;
    ALTER TABLE [XeroLedgerLines] ALTER COLUMN [Note] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924160000_WidenWrittenTextColumns')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924160000_WidenWrittenTextColumns', N'8.0.10');
END;
GO

COMMIT;
GO
