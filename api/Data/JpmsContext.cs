using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data;

public sealed class JpmsContext : DbContext
{
    public JpmsContext(DbContextOptions<JpmsContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    public DbSet<DirectoryUserEntity> DirectoryUsers => Set<DirectoryUserEntity>();
    public DbSet<DirectoryUserRoleEntity> DirectoryUserRoles => Set<DirectoryUserRoleEntity>();
    public DbSet<AccessRequestEntity> AccessRequests => Set<AccessRequestEntity>();

    public DbSet<UserCredentialEntity> UserCredentials => Set<UserCredentialEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    public DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<ProjectContactEntity> ProjectContacts => Set<ProjectContactEntity>();
    public DbSet<PartyContactEntity> PartyContacts => Set<PartyContactEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ArchitectEntity> Architects => Set<ArchitectEntity>();
    public DbSet<LeadEntity> Leads => Set<LeadEntity>();
    public DbSet<QualificationAssessmentEntity> QualificationAssessments => Set<QualificationAssessmentEntity>();
    public DbSet<SiteVisitEntity> SiteVisits => Set<SiteVisitEntity>();
    public DbSet<InfoChaseItemEntity> InfoChaseItems => Set<InfoChaseItemEntity>();
    public DbSet<BidDecisionEntity> BidDecisions => Set<BidDecisionEntity>();
    public DbSet<ProposalEntity> Proposals => Set<ProposalEntity>();
    public DbSet<LeadOutcomeEntity> LeadOutcomes => Set<LeadOutcomeEntity>();

    public DbSet<BoqLineItemEntity> BoqLineItems => Set<BoqLineItemEntity>();
    public DbSet<BoqSignOffEntity> BoqSignOffs => Set<BoqSignOffEntity>();
    public DbSet<RateEntity> Rates => Set<RateEntity>();
    public DbSet<CostCodeEntity> CostCodes => Set<CostCodeEntity>();
    public DbSet<WalkRoundNoteEntity> WalkRoundNotes => Set<WalkRoundNoteEntity>();

    public DbSet<DrawingEntity> Drawings => Set<DrawingEntity>();
    public DbSet<DrawingRevisionEntity> DrawingRevisions => Set<DrawingRevisionEntity>();
    public DbSet<DrawingIssueRecordEntity> DrawingIssueRecords => Set<DrawingIssueRecordEntity>();

    public DbSet<SubcontractorEntity> Subcontractors => Set<SubcontractorEntity>();
    public DbSet<TradeEntity> Trades => Set<TradeEntity>();
    public DbSet<SubcontractorTradeEntity> SubcontractorTrades => Set<SubcontractorTradeEntity>();
    public DbSet<ComplianceDocumentEntity> ComplianceDocuments => Set<ComplianceDocumentEntity>();

    public DbSet<HsRecordEntity> HsRecords => Set<HsRecordEntity>();
    public DbSet<HsRecordAttendanceEntity> HsRecordAttendance => Set<HsRecordAttendanceEntity>();
    public DbSet<MobilisationItemEntity> MobilisationItems => Set<MobilisationItemEntity>();

    public DbSet<BidPackageEntity> BidPackages => Set<BidPackageEntity>();
    public DbSet<BidPackageRecipientEntity> BidPackageRecipients => Set<BidPackageRecipientEntity>();
    public DbSet<BidPackageLineItemEntity> BidPackageLineItems => Set<BidPackageLineItemEntity>();
    public DbSet<QuoteEntity> Quotes => Set<QuoteEntity>();
    public DbSet<QuoteLineItemEntity> QuoteLineItems => Set<QuoteLineItemEntity>();
    public DbSet<BidPackageDrawingEntity> BidPackageDrawings => Set<BidPackageDrawingEntity>();
    public DbSet<WorkOrderEntity> WorkOrders => Set<WorkOrderEntity>();
    public DbSet<WorkOrderLineEntity> WorkOrderLines => Set<WorkOrderLineEntity>();
    public DbSet<VariationOrderEntity> VariationOrders => Set<VariationOrderEntity>();
    public DbSet<SubcontractorVariationRequestEntity> SubcontractorVariationRequests => Set<SubcontractorVariationRequestEntity>();
    public DbSet<RequestEntity> Requests => Set<RequestEntity>();
    public DbSet<RequestItemEntity> RequestItems => Set<RequestItemEntity>();
    public DbSet<RequestMessageEntity> RequestMessages => Set<RequestMessageEntity>();
    public DbSet<RequestAgentEntity> RequestAgents => Set<RequestAgentEntity>();
    public DbSet<AgentChatMessageEntity> AgentChatMessages => Set<AgentChatMessageEntity>();
    public DbSet<AgentProposalEntity> AgentProposals => Set<AgentProposalEntity>();
    public DbSet<CostCenterEntity> CostCenters => Set<CostCenterEntity>();

    public DbSet<XeroLedgerLineEntity> XeroLedgerLines => Set<XeroLedgerLineEntity>();
    public DbSet<XeroCostSplitEntity> XeroCostSplits => Set<XeroCostSplitEntity>();
    public DbSet<XeroLineWorkOrderLinkEntity> XeroLineWorkOrderLinks => Set<XeroLineWorkOrderLinkEntity>();

    public DbSet<TodoItemEntity> TodoItems => Set<TodoItemEntity>();

    public DbSet<LadClaimEntity> LadClaims => Set<LadClaimEntity>();

    public DbSet<SiteReportEntity> SiteReports => Set<SiteReportEntity>();
    public DbSet<ProgrammeTaskEntity> ProgrammeTasks => Set<ProgrammeTaskEntity>();
    public DbSet<ProgrammeTaskLinkEntity> ProgrammeTaskLinks => Set<ProgrammeTaskLinkEntity>();
    public DbSet<ProgrammeBaselineEntity> ProgrammeBaselines => Set<ProgrammeBaselineEntity>();
    public DbSet<ProgrammeBaselineTaskEntity> ProgrammeBaselineTasks => Set<ProgrammeBaselineTaskEntity>();
    public DbSet<PhotoEntity> Photos => Set<PhotoEntity>();

    public DbSet<ProgressUpdateEntity> ProgressUpdates => Set<ProgressUpdateEntity>();
    public DbSet<ProgressPhotoEntity> ProgressPhotos => Set<ProgressPhotoEntity>();
    public DbSet<ProgressReportEntity> ProgressReports => Set<ProgressReportEntity>();
    public DbSet<ProgressReportSelectionEntity> ProgressReportSelections => Set<ProgressReportSelectionEntity>();

    public DbSet<ClaimPeriodEntity> ClaimPeriods => Set<ClaimPeriodEntity>();
    public DbSet<ValuationEntity> Valuations => Set<ValuationEntity>();
    public DbSet<ValuationLineItemEntity> ValuationLineItems => Set<ValuationLineItemEntity>();
    public DbSet<ValuationClaimEntity> ValuationClaims => Set<ValuationClaimEntity>();
    public DbSet<ClaimLineEntity> ClaimLines => Set<ClaimLineEntity>();
    public DbSet<CvrSnapshotEntity> CvrSnapshots => Set<CvrSnapshotEntity>();
    public DbSet<CvrPackageRowEntity> CvrPackageRows => Set<CvrPackageRowEntity>();
    public DbSet<ForecastComponentEntity> ForecastComponents => Set<ForecastComponentEntity>();
    public DbSet<QsAccrualEntity> QsAccruals => Set<QsAccrualEntity>();
    public DbSet<PrelimItemEntity> PrelimItems => Set<PrelimItemEntity>();
    public DbSet<PrelimForecastEntryEntity> PrelimForecastEntries => Set<PrelimForecastEntryEntity>();
    public DbSet<EotEntity> Eots => Set<EotEntity>();
    public DbSet<CostCodeBudgetEntity> CostCodeBudgets => Set<CostCodeBudgetEntity>();
    public DbSet<CostCentreCostProgressEntity> CostCentreCostProgress => Set<CostCentreCostProgressEntity>();
    public DbSet<CostCentreGroupEntity> CostCentreGroups => Set<CostCentreGroupEntity>();
    public DbSet<CostCentreGroupMemberEntity> CostCentreGroupMembers => Set<CostCentreGroupMemberEntity>();
    public DbSet<ReconciliationPackageEntity> ReconciliationPackages => Set<ReconciliationPackageEntity>();
    public DbSet<ReconciliationPackageOrderEntity> ReconciliationPackageOrders => Set<ReconciliationPackageOrderEntity>();
    public DbSet<ReconciliationPackageSalesLineEntity> ReconciliationPackageSalesLines => Set<ReconciliationPackageSalesLineEntity>();
    public DbSet<ReconciliationPackageCostLineEntity> ReconciliationPackageCostLines => Set<ReconciliationPackageCostLineEntity>();
    public DbSet<TimesheetEntity> Timesheets => Set<TimesheetEntity>();
    public DbSet<WorkerEntity> Workers => Set<WorkerEntity>();
    public DbSet<WorkerRateHistoryEntity> WorkerRateHistories => Set<WorkerRateHistoryEntity>();
    public DbSet<ProjectWorkerAssignmentEntity> ProjectWorkerAssignments => Set<ProjectWorkerAssignmentEntity>();
    public DbSet<SiteAttendanceEntity> SiteAttendances => Set<SiteAttendanceEntity>();
    public DbSet<XeroLineTimesheetCoverEntity> XeroLineTimesheetCovers => Set<XeroLineTimesheetCoverEntity>();
    public DbSet<LabourSettlementVarianceEntity> LabourSettlementVariances => Set<LabourSettlementVarianceEntity>();
    public DbSet<CashflowSnapshotEntity> CashflowSnapshots => Set<CashflowSnapshotEntity>();
    public DbSet<ValuationInvoiceEntity> ValuationInvoices => Set<ValuationInvoiceEntity>();
    public DbSet<ValuationInvoiceEventEntity> ValuationInvoiceEvents => Set<ValuationInvoiceEventEntity>();
    public DbSet<ValuationReportSnapshotEntity> ValuationReportSnapshots => Set<ValuationReportSnapshotEntity>();
    public DbSet<ValuationReportSnapshotLineEntity> ValuationReportSnapshotLines => Set<ValuationReportSnapshotLineEntity>();
    public DbSet<DayworkEntity> Dayworks => Set<DayworkEntity>();
    public DbSet<ContraChargeEntity> ContraCharges => Set<ContraChargeEntity>();
    public DbSet<SubcontractorRetentionEntity> SubcontractorRetentions => Set<SubcontractorRetentionEntity>();
    public DbSet<ProjectRetentionEntity> ProjectRetentions => Set<ProjectRetentionEntity>();

    public DbSet<DefectEntity> Defects => Set<DefectEntity>();
    public DbSet<PracticalCompletionEntity> PracticalCompletions => Set<PracticalCompletionEntity>();
    public DbSet<HandoverPackItemEntity> HandoverPackItems => Set<HandoverPackItemEntity>();
    public DbSet<SettlementRecordEntity> SettlementRecords => Set<SettlementRecordEntity>();
    public DbSet<VatAnalysisEntity> VatAnalyses => Set<VatAnalysisEntity>();
    public DbSet<RetentionReleaseEntity> RetentionReleases => Set<RetentionReleaseEntity>();

    // Append-only audit trail of client-facing interactions (pathway split — see
    // docs/Pathway-Split-Platform-Flow-Plan.md §4).
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    /// <summary>
    /// Read-path indexes. JPMS deliberately declares no FK relationships (records link by loose
    /// string id), so EF's automatic FK-index convention never fires — every ProjectId / RequestId
    /// / WorkOrderId lookup was a table scan that grows with the data. These are the columns the
    /// hot queries actually filter and join on; each is declared here so the model, the snapshot
    /// and the database agree, and created by the AddPerformanceIndexes migration.
    ///
    /// Index names are pinned explicitly because three of them (WorkOrderLines, Timesheets,
    /// XeroLineTimesheetCovers) already exist in production — hand-run from
    /// infra/perf-financials-indexes.sql after the financials query started returning 504s — so
    /// the migration must not collide with them. Those three carry INCLUDE columns in the database
    /// that are not modelled here; INCLUDE is a storage detail EF never validates at runtime.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Auth: read on every single authenticated request ---------------------------------
        // (UserSessions.SessionId and DirectoryUsers.Email are primary keys, so those two lookups
        // already seek; the role list was the one that scanned.)
        modelBuilder.Entity<DirectoryUserRoleEntity>()
            .HasIndex(row => row.DirectoryUserEmail)
            .HasDatabaseName("IX_DirectoryUserRoles_DirectoryUserEmail");

        // ---- Requests / RFIs -------------------------------------------------------------------
        modelBuilder.Entity<RequestEntity>()
            .HasIndex(row => new { row.ProjectId, row.Status })
            .HasDatabaseName("IX_Requests_ProjectId_Status");
        modelBuilder.Entity<RequestEntity>()
            .HasIndex(row => new { row.Kind, row.Status })
            .HasDatabaseName("IX_Requests_Kind_Status");
        modelBuilder.Entity<RequestMessageEntity>()
            .HasIndex(row => row.RequestId)
            .HasDatabaseName("IX_RequestMessages_RequestId");

        // ---- Variations ------------------------------------------------------------------------
        modelBuilder.Entity<VariationOrderEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_VariationOrderQuotes_ProjectId");
        modelBuilder.Entity<VariationOrderEntity>()
            .HasIndex(row => row.RequestId)
            .HasDatabaseName("IX_VariationOrderQuotes_RequestId");

        // ---- Procurement -----------------------------------------------------------------------
        modelBuilder.Entity<WorkOrderEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_WorkOrders_ProjectId");
        modelBuilder.Entity<WorkOrderEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_WorkOrders_VariationOrderId");
        modelBuilder.Entity<WorkOrderLineEntity>()
            .HasIndex(row => row.WorkOrderId)
            .HasDatabaseName("IX_WorkOrderLines_WorkOrderId");
        modelBuilder.Entity<BidPackageEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_BidPackages_ProjectId");
        modelBuilder.Entity<BidPackageEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_BidPackages_VariationOrderQuoteId");
        modelBuilder.Entity<QuoteEntity>()
            .HasIndex(row => row.BidPackageId)
            .HasDatabaseName("IX_Quotes_BidPackageId");

        // ---- Labour / financials (the first two already live in production) ---------------------
        modelBuilder.Entity<TimesheetEntity>()
            .HasIndex(row => new { row.ProjectId, row.Status })
            .HasDatabaseName("IX_Timesheets_ProjectId_Status");
        modelBuilder.Entity<XeroLineTimesheetCoverEntity>()
            .HasIndex(row => row.XeroLedgerLineId)
            .HasDatabaseName("IX_XeroLineTimesheetCovers_XeroLedgerLineId");
        modelBuilder.Entity<SiteAttendanceEntity>()
            .HasIndex(row => new { row.ProjectId, row.WorkDate })
            .HasDatabaseName("IX_SiteAttendances_ProjectId_WorkDate");

        // ---- Project-scoped registers -----------------------------------------------------------
        modelBuilder.Entity<DrawingEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_Drawings_ProjectId");
        modelBuilder.Entity<DrawingRevisionEntity>()
            .HasIndex(row => row.DrawingId)
            .HasDatabaseName("IX_DrawingRevisions_DrawingId");
        modelBuilder.Entity<HsRecordEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_HsRecords_ProjectId");
        modelBuilder.Entity<TodoItemEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_TodoItems_ProjectId");
        modelBuilder.Entity<DefectEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_Defects_ProjectId");
        modelBuilder.Entity<ComplianceDocumentEntity>()
            .HasIndex(row => row.SubcontractorId)
            .HasDatabaseName("IX_ComplianceDocuments_SubcontractorId");
    }
}
