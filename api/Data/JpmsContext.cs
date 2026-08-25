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
    public DbSet<DrawingFolderEntity> DrawingFolders => Set<DrawingFolderEntity>();

    public DbSet<DocumentControlItemEntity> DocumentControlItems => Set<DocumentControlItemEntity>();
    public DbSet<PaymentCertificateEntity> PaymentCertificates => Set<PaymentCertificateEntity>();

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
    public DbSet<BidPackageAttachmentEntity> BidPackageAttachments => Set<BidPackageAttachmentEntity>();
    public DbSet<TenderEnquiryEntity> TenderEnquiries => Set<TenderEnquiryEntity>();
    public DbSet<TenderEnquiryAnswerEntity> TenderEnquiryAnswers => Set<TenderEnquiryAnswerEntity>();
    public DbSet<TenderEnquiryAttachmentEntity> TenderEnquiryAttachments => Set<TenderEnquiryAttachmentEntity>();
    public DbSet<WorkOrderEntity> WorkOrders => Set<WorkOrderEntity>();
    public DbSet<WorkOrderLineEntity> WorkOrderLines => Set<WorkOrderLineEntity>();
    public DbSet<WorkOrderAttachmentEntity> WorkOrderAttachments => Set<WorkOrderAttachmentEntity>();
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
    // The discussion thread on disputed ledger lines (the allocation page's Disputed bucket).
    public DbSet<XeroDisputeMessageEntity> XeroDisputeMessages => Set<XeroDisputeMessageEntity>();

    // The stored site P&L: per project per month, from Xero's profit & loss report filtered by
    // the project's Sites tracking option — feeds the Profit Summary's cumulative chart.
    public DbSet<XeroSitePnlMonthEntity> XeroSitePnlMonths => Set<XeroSitePnlMonthEntity>();

    public DbSet<TodoItemEntity> TodoItems => Set<TodoItemEntity>();
    // Undirected to-do ↔ to-do links, one row per pair in canonical id order (TodoItemLinkPairs).
    public DbSet<TodoItemLinkEntity> TodoItemLinks => Set<TodoItemLinkEntity>();
    // The per-item timeline (created / started / chased / reassigned / … / email sent).
    public DbSet<TodoItemActivityEntity> TodoItemActivities => Set<TodoItemActivityEntity>();

    // Internal-only titled free-text notes per project (door codes, site notes) — the Useful
    // Information tab.
    public DbSet<UsefulInformationNoteEntity> UsefulInformationNotes => Set<UsefulInformationNoteEntity>();

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
    public DbSet<WorkerContractEntity> WorkerContracts => Set<WorkerContractEntity>();
    public DbSet<WorkerAbsenceEntity> WorkerAbsences => Set<WorkerAbsenceEntity>();
    public DbSet<WorkerCisStatusEntity> WorkerCisStatuses => Set<WorkerCisStatusEntity>();
    public DbSet<LabourWeekSignOffEntity> LabourWeekSignOffs => Set<LabourWeekSignOffEntity>();
    public DbSet<WorkerSettlementLineEntity> WorkerSettlementLines => Set<WorkerSettlementLineEntity>();
    public DbSet<SiteXeroMappingEntity> SiteXeroMappings => Set<SiteXeroMappingEntity>();
    public DbSet<CostCodeXeroMappingEntity> CostCodeXeroMappings => Set<CostCodeXeroMappingEntity>();
    public DbSet<XeroCodingRunEntity> XeroCodingRuns => Set<XeroCodingRunEntity>();
    public DbSet<CompanyRegisterItemEntity> CompanyRegisterItems => Set<CompanyRegisterItemEntity>();
    public DbSet<PolicyDocumentEntity> PolicyDocuments => Set<PolicyDocumentEntity>();
    public DbSet<PolicySignOffEntity> PolicySignOffs => Set<PolicySignOffEntity>();
    public DbSet<CashflowSnapshotEntity> CashflowSnapshots => Set<CashflowSnapshotEntity>();
    public DbSet<ValuationInvoiceEntity> ValuationInvoices => Set<ValuationInvoiceEntity>();
    public DbSet<ValuationInvoiceEventEntity> ValuationInvoiceEvents => Set<ValuationInvoiceEventEntity>();
    public DbSet<ValuationReportSnapshotEntity> ValuationReportSnapshots => Set<ValuationReportSnapshotEntity>();
    public DbSet<ValuationReportSnapshotLineEntity> ValuationReportSnapshotLines => Set<ValuationReportSnapshotLineEntity>();
    public DbSet<ClientCostReferenceEntity> ClientCostReferences => Set<ClientCostReferenceEntity>();
    public DbSet<DayworkEntity> Dayworks => Set<DayworkEntity>();
    public DbSet<ContraChargeEntity> ContraCharges => Set<ContraChargeEntity>();
    public DbSet<SubcontractorRetentionEntity> SubcontractorRetentions => Set<SubcontractorRetentionEntity>();
    public DbSet<ProjectRetentionEntity> ProjectRetentions => Set<ProjectRetentionEntity>();
    public DbSet<ProjectContractEntity> ProjectContracts => Set<ProjectContractEntity>();
    public DbSet<ProjectContractAmendmentEntity> ProjectContractAmendments => Set<ProjectContractAmendmentEntity>();

    public DbSet<AiConversationEntity> AiConversations => Set<AiConversationEntity>();
    public DbSet<AiConversationMessageEntity> AiConversationMessages => Set<AiConversationMessageEntity>();
    public DbSet<AgentActivityEntity> AgentActivity => Set<AgentActivityEntity>();

    // The assistant's skills — the domain half of an agent, edited in the portal
    // (docs/ai/05-agents-and-skills.md). Revisions are append-only.
    public DbSet<SkillEntity> Skills => Set<SkillEntity>();
    public DbSet<SkillReferenceEntity> SkillReferences => Set<SkillReferenceEntity>();
    public DbSet<SkillRevisionEntity> SkillRevisions => Set<SkillRevisionEntity>();

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

        // ---- Assistant skills --------------------------------------------------------------------
        // Every turn loads the agent's skills plus the shared set, so the agent-key filter is the
        // hot path. RefKey is unique per skill — the model asks for a reference by that pair.
        modelBuilder.Entity<SkillEntity>()
            .HasIndex(row => new { row.AgentKey, row.IsActive })
            .HasDatabaseName("IX_Skills_AgentKey_IsActive");
        modelBuilder.Entity<SkillReferenceEntity>()
            .HasIndex(row => new { row.SkillKey, row.RefKey })
            .IsUnique()
            .HasDatabaseName("IX_SkillReferences_SkillKey_RefKey");
        modelBuilder.Entity<SkillRevisionEntity>()
            .HasIndex(row => new { row.SkillKey, row.Version })
            .HasDatabaseName("IX_SkillRevisions_SkillKey_Version");

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
        // Amendments are always read per project. NOT unique — they accumulate.
        modelBuilder.Entity<ProjectContractAmendmentEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_ProjectContractAmendments_ProjectId");

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
        modelBuilder.Entity<XeroDisputeMessageEntity>()
            .HasIndex(row => row.XeroLedgerLineId)
            .HasDatabaseName("IX_XeroDisputeMessages_XeroLedgerLineId");
        modelBuilder.Entity<SiteAttendanceEntity>()
            .HasIndex(row => new { row.ProjectId, row.WorkDate })
            .HasDatabaseName("IX_SiteAttendances_ProjectId_WorkDate");
        modelBuilder.Entity<WorkerAbsenceEntity>()
            .HasIndex(row => new { row.WorkerId, row.Date })
            .IsUnique()
            .HasDatabaseName("IX_WorkerAbsences_WorkerId_Date");
        modelBuilder.Entity<LabourWeekSignOffEntity>()
            .HasIndex(row => new { row.WorkerId, row.WeekStart })
            .IsUnique()
            .HasDatabaseName("IX_LabourWeekSignOffs_WorkerId_WeekStart");
        modelBuilder.Entity<WorkerContractEntity>()
            .HasIndex(row => row.WorkerId)
            .HasDatabaseName("IX_WorkerContracts_WorkerId");
        modelBuilder.Entity<WorkerCisStatusEntity>()
            .HasIndex(row => row.WorkerId)
            .HasDatabaseName("IX_WorkerCisStatuses_WorkerId");
        modelBuilder.Entity<WorkerSettlementLineEntity>()
            .HasIndex(row => new { row.WorkerId, row.Month })
            .HasDatabaseName("IX_WorkerSettlementLines_WorkerId_Month");
        modelBuilder.Entity<SiteXeroMappingEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_SiteXeroMappings_ProjectId");
        modelBuilder.Entity<CostCodeXeroMappingEntity>()
            .HasIndex(row => row.CostCode)
            .HasDatabaseName("IX_CostCodeXeroMappings_CostCode");
        modelBuilder.Entity<XeroCodingRunEntity>()
            .HasIndex(row => new { row.WorkerId, row.Month })
            .HasDatabaseName("IX_XeroCodingRuns_WorkerId_Month");
        modelBuilder.Entity<CompanyRegisterItemEntity>()
            .HasIndex(row => row.Kind)
            .HasDatabaseName("IX_CompanyRegisterItems_Kind");
        modelBuilder.Entity<PolicySignOffEntity>()
            .HasIndex(row => new { row.PolicyDocumentId, row.RecipientEmail })
            .IsUnique()
            .HasDatabaseName("IX_PolicySignOffs_PolicyDocumentId_RecipientEmail");

        // ---- Project-scoped registers -----------------------------------------------------------
        modelBuilder.Entity<DrawingEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_Drawings_ProjectId");
        modelBuilder.Entity<DrawingRevisionEntity>()
            .HasIndex(row => row.DrawingId)
            .HasDatabaseName("IX_DrawingRevisions_DrawingId");
        modelBuilder.Entity<DrawingFolderEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_DrawingFolders_ProjectId");
        modelBuilder.Entity<DrawingFolderEntity>()
            .HasIndex(row => row.ParentDrawingFolderId)
            .HasDatabaseName("IX_DrawingFolders_ParentDrawingFolderId");
        modelBuilder.Entity<HsRecordEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_HsRecords_ProjectId");
        modelBuilder.Entity<TodoItemEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_TodoItems_ProjectId");
        modelBuilder.Entity<UsefulInformationNoteEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_UsefulInformationNotes_ProjectId");
        // Unique on the canonically-ordered pair: the same two items can only be linked once, and
        // "everything linked to X" seeks this index for the A side and the one below for the B side.
        modelBuilder.Entity<TodoItemLinkEntity>()
            .HasIndex(row => new { row.TodoItemAId, row.TodoItemBId })
            .IsUnique()
            .HasDatabaseName("IX_TodoItemLinks_TodoItemAId_TodoItemBId");
        modelBuilder.Entity<TodoItemLinkEntity>()
            .HasIndex(row => row.TodoItemBId)
            .HasDatabaseName("IX_TodoItemLinks_TodoItemBId");
        modelBuilder.Entity<TodoItemActivityEntity>()
            .HasIndex(row => row.TodoItemId)
            .HasDatabaseName("IX_TodoItemActivities_TodoItemId");
        // One client reference per cost centre per project.
        modelBuilder.Entity<ClientCostReferenceEntity>()
            .HasIndex(row => new { row.ProjectId, row.CostCode })
            .IsUnique()
            .HasDatabaseName("IX_ClientCostReferences_ProjectId_CostCode");
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
        // Work-order attachments are always read per order (the PO page's panel and the
        // create-form uploads land through the same list).
        modelBuilder.Entity<WorkOrderAttachmentEntity>()
            .HasIndex(row => row.WorkOrderId)
            .HasDatabaseName("IX_WorkOrderAttachments_WorkOrderId");
        // Bid-package attachments are read per package (the Documents section and the invite draft).
        modelBuilder.Entity<BidPackageAttachmentEntity>()
            .HasIndex(row => row.BidPackageId)
            .HasDatabaseName("IX_BidPackageAttachments_BidPackageId");

        // ---- Tender enquiries -----------------------------------------------------------------------
        // Read per project (the Tender Enquiries tab) and by number (the TEQ-#### tag resolves back
        // to its record); answers and attachments are read per enquiry.
        modelBuilder.Entity<TenderEnquiryEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_TenderEnquiries_ProjectId");
        modelBuilder.Entity<TenderEnquiryEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_TenderEnquiries_Number");
        modelBuilder.Entity<TenderEnquiryAnswerEntity>()
            .HasIndex(row => row.TenderEnquiryId)
            .HasDatabaseName("IX_TenderEnquiryAnswers_TenderEnquiryId");
        modelBuilder.Entity<TenderEnquiryAttachmentEntity>()
            .HasIndex(row => row.TenderEnquiryId)
            .HasDatabaseName("IX_TenderEnquiryAttachments_TenderEnquiryId");

        // ---- Audit trail ---------------------------------------------------------------------------
        // The register is read per record (a request's own History panel) as well as per project.
        modelBuilder.Entity<AuditEventEntity>()
            .HasIndex(row => row.RecordId)
            .HasDatabaseName("IX_AuditEvents_RecordId");

        // ---- Site P&L ------------------------------------------------------------------------------
        modelBuilder.Entity<XeroSitePnlMonthEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_XeroSitePnlMonths_ProjectId");

        // ---- Document Control ----------------------------------------------------------------------
        // The send handler's "already sent?" read seeks on MessageId. (No UNIQUE composite over
        // MessageId+AttachmentId: two nvarchar(512) Graph ids overshoot SQL Server's 1700-byte
        // index-key cap, so one-row-per-attachment is enforced in the handler instead.)
        modelBuilder.Entity<DocumentControlItemEntity>()
            .HasIndex(row => row.MessageId)
            .HasDatabaseName("IX_DocumentControlItems_MessageId");
        modelBuilder.Entity<DocumentControlItemEntity>()
            .HasIndex(row => row.Status)
            .HasDatabaseName("IX_DocumentControlItems_Status");
        modelBuilder.Entity<PaymentCertificateEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_PaymentCertificates_ProjectId");
    }
}
