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
    public DbSet<SubcontractorXeroLinkEntity> SubcontractorXeroLinks => Set<SubcontractorXeroLinkEntity>();
    public DbSet<CompanyContactEntity> CompanyContacts => Set<CompanyContactEntity>();
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
    public DbSet<ProjectContractEntity> ProjectContracts => Set<ProjectContractEntity>();

    public DbSet<AiConversationEntity> AiConversations => Set<AiConversationEntity>();
    public DbSet<AiConversationMessageEntity> AiConversationMessages => Set<AiConversationMessageEntity>();
    public DbSet<AgentActivityEntity> AgentActivity => Set<AgentActivityEntity>();

    public DbSet<DefectEntity> Defects => Set<DefectEntity>();
    public DbSet<PracticalCompletionEntity> PracticalCompletions => Set<PracticalCompletionEntity>();
    public DbSet<HandoverPackItemEntity> HandoverPackItems => Set<HandoverPackItemEntity>();
    public DbSet<SettlementRecordEntity> SettlementRecords => Set<SettlementRecordEntity>();
    public DbSet<VatAnalysisEntity> VatAnalyses => Set<VatAnalysisEntity>();
    public DbSet<RetentionReleaseEntity> RetentionReleases => Set<RetentionReleaseEntity>();

    // Append-only audit trail of client-facing interactions (pathway split — see
    // docs/Pathway-Split-Platform-Flow-Plan.md §4).
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    // The announced app version — one row ("current") that Admin → System publishes and every
    // HTTP response header reports; see Features/Platform.
    public DbSet<AppVersionEntity> AppVersions => Set<AppVersionEntity>();

    // Architect's Instructions — the formal instructions that authorise varied work — plus the
    // many-to-many between them and the variations they cover.
    public DbSet<ArchitectInstructionEntity> ArchitectInstructions => Set<ArchitectInstructionEntity>();
    public DbSet<ArchitectInstructionVariationEntity> ArchitectInstructionVariations =>
        Set<ArchitectInstructionVariationEntity>();

    // Drawings and files attached to a request (site photos, marked-up details, linked revisions).
    public DbSet<RequestAttachmentEntity> RequestAttachments => Set<RequestAttachmentEntity>();

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

        // ---- Agent activity log ----------------------------------------------------------------
        // Read newest-first, and the filter that matters most is "only what ran unattended".
        modelBuilder.Entity<AgentActivityEntity>()
            .HasIndex(row => row.OccurredAt)
            .HasDatabaseName("IX_AgentActivity_OccurredAt");
        modelBuilder.Entity<AgentActivityEntity>()
            .HasIndex(row => new { row.IsAutonomous, row.OccurredAt })
            .HasDatabaseName("IX_AgentActivity_IsAutonomous_OccurredAt");
        modelBuilder.Entity<AgentActivityEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_AgentActivity_ProjectId");

        // ---- Assistant conversations -----------------------------------------------------------
        // Every turn replays the whole conversation in sequence order, so this index is the hot path.
        modelBuilder.Entity<AiConversationMessageEntity>()
            .HasIndex(row => new { row.ConversationId, row.Sequence })
            .HasDatabaseName("IX_AiConversationMessages_ConversationId_Sequence");
        modelBuilder.Entity<AiConversationEntity>()
            .HasIndex(row => new { row.StartedByEmail, row.LastMessageAt })
            .HasDatabaseName("IX_AiConversations_StartedByEmail_LastMessageAt");
        // "Which conversations drafted this variation / this RFI", newest first — the lookup an
        // argument about a document starts from.
        modelBuilder.Entity<AiConversationEntity>()
            .HasIndex(row => new { row.ScopeRecordId, row.LastMessageAt })
            .HasDatabaseName("IX_AiConversations_ScopeRecordId");

        // ---- Company directory (Xero links + contacts) -----------------------------------------
        // Unique: one Xero supplier can only ever be imported once — consolidation re-points the
        // link rather than duplicating it.
        modelBuilder.Entity<SubcontractorXeroLinkEntity>()
            .HasIndex(row => row.XeroContactId)
            .IsUnique()
            .HasDatabaseName("IX_SubcontractorXeroLinks_XeroContactId");
        modelBuilder.Entity<SubcontractorXeroLinkEntity>()
            .HasIndex(row => row.SubcontractorId)
            .HasDatabaseName("IX_SubcontractorXeroLinks_SubcontractorId");
        modelBuilder.Entity<CompanyContactEntity>()
            .HasIndex(row => row.SubcontractorId)
            .HasDatabaseName("IX_CompanyContacts_SubcontractorId");

        // ---- Project contracts -----------------------------------------------------------------
        // Unique: one contract per project. The handlers treat the row as an upsert, and this index
        // is what stops two concurrent first-saves from both inserting.
        modelBuilder.Entity<ProjectContractEntity>()
            .HasIndex(row => row.ProjectId)
            .IsUnique()
            .HasDatabaseName("IX_ProjectContracts_ProjectId");

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

        // ---- Architect's Instructions ------------------------------------------------------------
        modelBuilder.Entity<ArchitectInstructionEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_ArchitectInstructions_ProjectId");
        modelBuilder.Entity<ArchitectInstructionVariationEntity>()
            .HasIndex(row => row.ArchitectInstructionId)
            .HasDatabaseName("IX_ArchitectInstructionVariations_ArchitectInstructionId");
        modelBuilder.Entity<ArchitectInstructionVariationEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_ArchitectInstructionVariations_VariationOrderId");

        // ---- Request attachments ------------------------------------------------------------------
        modelBuilder.Entity<RequestAttachmentEntity>()
            .HasIndex(row => row.RequestId)
            .HasDatabaseName("IX_RequestAttachments_RequestId");

        // ---- Audit trail ---------------------------------------------------------------------------
        // The register is read per record (a request's own History panel) as well as per project.
        modelBuilder.Entity<AuditEventEntity>()
            .HasIndex(row => row.RecordId)
            .HasDatabaseName("IX_AuditEvents_RecordId");
    }
}
