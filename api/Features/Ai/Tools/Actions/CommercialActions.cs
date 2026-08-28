using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Commercial, CommercialInputs, CVR and Cashflow commands as connector actions.
/// Mirrors Features/Commercial/Commands, Features/CommercialInputs/Commands,
/// Features/Cvr/Commands and Features/Cashflow/Commands. Every authorisation in these areas
/// keeps its role set as a private field, so each VisibleTo below replicates the identical
/// roles with RoleSet.Of(...) — the field name comments say which authorisation each copies.
/// None of these endpoints stamp the signed-in user onto the command, so every entry's
/// stamp lists are empty.</summary>
internal sealed class CommercialActions : IAiActionSource
{
    // Replica of AddClaimPeriodAuthorisation.RolesThatMayDefineClaimPeriods.
    private static readonly RoleSet ClaimPeriodDefiners =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of ValuationReportAuthorisation.RolesThatMayEditValuationBill.
    private static readonly RoleSet ValuationBillEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of ValuationReportAuthorisation.RolesThatMayManageClaimLifecycle (identical to
    // its RolesThatMayManageSnapshots, RolesThatMayRecordClaimEntries and
    // RolesThatMayMapClientReferences sets).
    private static readonly RoleSet ClaimLifecycleManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.FinanceDirector);

    // Replica of ValuationReportAuthorisation.RolesThatMayRecodeCostCentres.
    private static readonly RoleSet CostCentreRecoders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // Replica of ApproveTimesheetAuthorisation.RolesThatMayApproveTimesheets.
    private static readonly RoleSet TimesheetApprovers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replica of SubmitTimesheetAuthorisation.RolesThatMaySubmitTimesheets.
    private static readonly RoleSet TimesheetSubmitters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Subcontractor);

    // Replica of CreateCostCentreGroupAuthorisation.RolesThatMayManageGroups,
    // ReconciliationPackageAuthorisation.RolesThatMayManagePackages,
    // SetCostCentreCostCompletionAuthorisation.RolesThatMaySetCostCompletion and
    // SetCostCentreFinalisationAuthorisation.RolesThatMayFinalise (all identical).
    private static readonly RoleSet FinancialsTabManagers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of DraftValuationAuthorisation.RolesThatMayDraftValuations (identical to
    // ReviseValuationAuthorisation.RolesThatMayReviseValuations and
    // SetCostCodeBudgetAuthorisation.RolesThatMaySetBudgets).
    private static readonly RoleSet ValuationDrafters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of IssueValuationAuthorisation.RolesThatMayIssueValuations (identical to
    // GrantEotAuthorisation.RolesThatMayGrantEots and UpdateEotAuthorisation.RolesThatMayUpdateEots).
    private static readonly RoleSet DirectorsOnly = RoleSet.Of(JpmsRoles.Director);

    // Replica of PrepareValuationReportSnapshotEmailDraftAuthorisation.RolesThatMayEmailSnapshots.
    private static readonly RoleSet SnapshotEmailDrafters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // Replica of SetXeroLineWorkOrderLinksAuthorisation.RolesThatMayLink.
    private static readonly RoleSet XeroWorkOrderLinkers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of LogDayworkAuthorisation.RolesThatMayLogDayworks.
    private static readonly RoleSet DayworkLoggers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager);

    // Replica of RecordContraChargeAuthorisation.RolesThatMayRecordContraCharges.
    private static readonly RoleSet ContraChargeRecorders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of RecordSubcontractorRetentionAuthorisation.RolesThatMayRecordRetention.
    private static readonly RoleSet RetentionRecorders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    // Replica of CaptureCvrSnapshotAuthorisation.RolesThatMayCaptureSnapshots (identical to the
    // RecordCvrPackageRow, RecordForecastComponent, RecordPrelimForecastForWeek, RecordQsAccrual
    // and UpdateQsAccrual authorisation sets).
    private static readonly RoleSet CvrEditors = RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    // Replica of CaptureCashflowSnapshotAuthorisation.RolesThatMayCaptureCashflow.
    private static readonly RoleSet CashflowCapturers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ── Commercial: claim periods, valuations and the valuation report bill ──────────

        new AiAction(
            Name: "add_claim_period",
            Area: "Commercial",
            Description: "Defines a numbered claim period (start and end dates) on a project — the "
                + "billing calendar valuations are drafted against.",
            CommandType: typeof(AddClaimPeriod),
            ResultType: typeof(ClaimPeriod),
            AuthorisationType: typeof(AddClaimPeriodAuthorisation),
            ValidationType: typeof(AddClaimPeriodValidation),
            VisibleTo: ClaimPeriodDefiners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are ISO 8601."),

        new AiAction(
            Name: "draft_valuation",
            Area: "Commercial",
            Description: "Creates a Draft valuation (gross value and retention percent) against one of "
                + "a project's claim periods — the money the company intends to certify for that period. "
                + "Draft only; nothing is issued to the client until issue_valuation.",
            CommandType: typeof(DraftValuation),
            ResultType: typeof(Valuation),
            AuthorisationType: typeof(DraftValuationAuthorisation),
            ValidationType: typeof(DraftValuationValidation),
            VisibleTo: ValuationDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; claimPeriodId from the project's claim periods "
                + "(add_claim_period creates them)."),

        new AiAction(
            Name: "revise_valuation",
            Area: "Commercial",
            Description: "Changes an existing valuation's gross value and retention percent — a direct "
                + "edit of the money figures on the valuation record.",
            CommandType: typeof(ReviseValuation),
            ResultType: typeof(Valuation),
            AuthorisationType: typeof(ReviseValuationAuthorisation),
            ValidationType: typeof(ReviseValuationValidation),
            VisibleTo: ValuationDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "valuationId comes from the project's valuations list."),

        new AiAction(
            Name: "issue_valuation",
            Area: "Commercial",
            Description: "Issues a drafted valuation — the formal act that moves it from Draft to "
                + "Issued, committing the certified money position for the period. Directors only.",
            CommandType: typeof(IssueValuation),
            ResultType: typeof(Valuation),
            AuthorisationType: typeof(IssueValuationAuthorisation),
            ValidationType: typeof(IssueValuationValidation),
            VisibleTo: DirectorsOnly,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — issuing is a formal financial step. "
                + "valuationId comes from the project's valuations list."),

        new AiAction(
            Name: "add_valuation_line_item",
            Area: "Commercial",
            Description: "Adds a priced line to a project's valuation report bill of quantities "
                + "(section, cost code, description, quantity, rate) — changing the total value the "
                + "report claims against.",
            CommandType: typeof(AddValuationLineItem),
            ResultType: typeof(ValuationLineItem),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(AddValuationLineItemValidation),
            VisibleTo: ValuationBillEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; cost codes from list_cost_codes. "
                + "get_valuation_context shows the existing bill and its sections."),

        new AiAction(
            Name: "update_valuation_line_item",
            Area: "Commercial",
            Description: "Rewrites one valuation report line's full details — section, cost code, "
                + "description, quantity, rate — changing the value that line contributes to the bill.",
            CommandType: typeof(UpdateValuationLineItem),
            ResultType: typeof(ValuationLineItem),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(UpdateValuationLineItemValidation),
            VisibleTo: ValuationBillEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This replaces every field on the line, not just the ones being changed — read the "
                + "current line first (get_valuation_context) and carry forward what should not change. "
                + "valuationLineItemId comes from the valuation report's lines."),

        new AiAction(
            Name: "remove_valuation_line_item",
            Area: "Commercial",
            Description: "Deletes a line from the valuation report bill permanently, removing its value "
                + "from the report. There is no undo.",
            CommandType: typeof(RemoveValuationLineItem),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationBillEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which line, by description and value, before calling."),

        // ── Commercial: valuation claim lifecycle ────────────────────────────────────────

        new AiAction(
            Name: "start_valuation_claim",
            Area: "Commercial",
            Description: "Starts a new valuation claim on a project (claim number and date), optionally "
                + "seeding every line's % complete from a previous claim. Retention terms are stamped "
                + "from the project's contract unless explicitly overridden.",
            CommandType: typeof(StartValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(StartValuationClaimValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Leave retentionPercent and "
                + "retentionReleasePercent null so the contract terms apply — a value is for "
                + "seeding/backfill only. seedFromClaimId (a previous claim's id) rolls the prior "
                + "claim's per-line % complete forward."),

        new AiAction(
            Name: "record_claim_entry",
            Area: "Commercial",
            Description: "Sets one valuation line's cumulative % complete on a Draft claim — the "
                + "commercial input that drives the amount claimed this period. The claim line's "
                + "cumulative claimed and period increment are recomputed from it.",
            CommandType: typeof(RecordClaimEntry),
            ResultType: typeof(ClaimLine),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(RecordClaimEntryValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Only works on a Draft claim. valuationClaimId and valuationLineItemId come from "
                + "get_valuation_context."),

        new AiAction(
            Name: "record_claim_entries",
            Area: "Commercial",
            Description: "Bulk-sets many valuation lines' cumulative % complete on a Draft claim in one "
                + "call — the same financial effect as record_claim_entry, batched for opening positions "
                + "or heavy-update months across large bills.",
            CommandType: typeof(RecordClaimEntries),
            ResultType: typeof(IReadOnlyList<ClaimLine>),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(RecordClaimEntriesValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Only works on a Draft claim. Each entry pairs a valuationLineItemId with its "
                + "cumulative percentComplete."),

        new AiAction(
            Name: "preapprove_valuation_claim",
            Area: "Commercial",
            Description: "Locks a Draft claim's amounts and moves it to Preapproved — the \"we are "
                + "claiming this\" step that freezes what will be put to the client. Reversible only "
                + "via reopen_valuation_claim.",
            CommandType: typeof(PreapproveValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — this freezes the claim's amounts. "
                + "valuationClaimId comes from get_valuation_context."),

        new AiAction(
            Name: "reopen_valuation_claim",
            Area: "Commercial",
            Description: "Undoes an unintended preapproval: moves a Preapproved claim back to Draft, "
                + "clearing the frozen totals so amounts compute live from entries again. Confirmed "
                + "claims are final and cannot be reopened.",
            CommandType: typeof(ReopenValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "confirm_valuation_claim",
            Area: "Commercial",
            Description: "Records that the client has paid: freezes the claim's summary totals and "
                + "per-row claimed amounts and advances the project's certified-to-date position, which "
                + "the next claim measures its increment from. Final — a Confirmed claim cannot be "
                + "reopened.",
            CommandType: typeof(ConfirmValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Irreversible. Confirm with the user, naming the claim, before calling."),

        new AiAction(
            Name: "rename_valuation_claim",
            Area: "Commercial",
            Description: "Sets a claim's free-text period name (e.g. \"June 2026\"). Bookkeeping only — "
                + "no amounts change, and a locked claim may still be renamed.",
            CommandType: typeof(RenameValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_valuation_claim",
            Area: "Commercial",
            Description: "Deletes a claim and its per-line entries permanently (test claims, false "
                + "starts). Invoices and snapshots that referenced it survive with the link cleared — "
                + "money already invoiced or certified does not move. There is no undo.",
            CommandType: typeof(DeleteValuationClaim),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which claim, by number and name, before calling."),

        // ── Commercial: valuation report snapshots ───────────────────────────────────────

        new AiAction(
            Name: "take_valuation_report_snapshot",
            Area: "Commercial",
            Description: "Freezes an immutable, line-level copy of the project's valuation report as it "
                + "stands right now — every priced line with % complete and cumulative claimed, plus the "
                + "summary and retention footer with certified-to-date stamped at this moment. The "
                + "period-end financial record; an amendment means taking a NEW snapshot.",
            CommandType: typeof(TakeValuationReportSnapshot),
            ResultType: typeof(ValuationReportSnapshot),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(TakeValuationReportSnapshotValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling, and agree the label. Leave "
                + "valuationInvoiceId null — it is set by the automatic capture behind an invoice "
                + "submission, not on-demand snapshots."),

        new AiAction(
            Name: "delete_valuation_report_snapshot",
            Area: "Commercial",
            Description: "Permanently removes a valuation report snapshot taken in error, with its "
                + "lines. Never touches live report data; any invoice pointing at it has its snapshot "
                + "link cleared. There is no undo.",
            CommandType: typeof(DeleteValuationReportSnapshot),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which snapshot, by label and date, before calling."),

        new AiAction(
            Name: "prepare_valuation_report_snapshot_email_draft",
            Area: "Commercial",
            Description: "Creates a DRAFT email in the shared mailbox addressed to the project's Client "
                + "and Architect contacts, with the frozen valuation report attached as a PDF — nothing "
                + "is sent; a human reviews and sends it from Outlook. The subject and HTML cover note "
                + "are supplied by the caller.",
            CommandType: typeof(PrepareValuationReportSnapshotEmailDraft),
            ResultType: typeof(ValuationReportSnapshotEmailDraft),
            AuthorisationType: typeof(PrepareValuationReportSnapshotEmailDraftAuthorisation),
            ValidationType: typeof(PrepareValuationReportSnapshotEmailDraftValidation),
            VisibleTo: SnapshotEmailDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This is client-facing money correspondence — confirm the subject and cover-note "
                + "wording with the user before calling. Recipients are fixed to the project's Client "
                + "and Architect contacts (projects@ is cc'd automatically). valuationReportSnapshotId "
                + "comes from the project's snapshots list."),

        // ── Commercial: Financials tab — budgets, cost centres, groups, packages ─────────

        new AiAction(
            Name: "set_cost_code_budget",
            Area: "Commercial",
            Description: "Sets a cost code's budget on a project — the allocated amount and spent "
                + "amount that the Financials tab reads. Upserts the budget row for that code.",
            CommandType: typeof(SetCostCodeBudget),
            ResultType: typeof(CostCodeBudget),
            AuthorisationType: typeof(SetCostCodeBudgetAuthorisation),
            ValidationType: typeof(SetCostCodeBudgetValidation),
            VisibleTo: ValuationDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; costCode from list_cost_codes."),

        new AiAction(
            Name: "set_cost_centre_cost_completion",
            Area: "Commercial",
            Description: "Sets the cost-side completion percentage for one cost centre on a project — "
                + "the commercial team's assessment of how far through the cost of the work they are, "
                + "shown on the Financials tab. Distinct from sales-side completion, which comes from "
                + "the latest claim. Upserts.",
            CommandType: typeof(SetCostCentreCostCompletion),
            ResultType: typeof(CostCentreCostProgress),
            AuthorisationType: typeof(SetCostCentreCostCompletionAuthorisation),
            ValidationType: typeof(SetCostCentreCostCompletionValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; costCode from list_cost_codes."),

        new AiAction(
            Name: "set_cost_centre_finalisation",
            Area: "Commercial",
            Description: "Locks a cost centre down on the Financials tab (or unlocks it). A finalised "
                + "centre expects no further spend: its remaining drawdown reads as realised profit or "
                + "loss instead of funds still available — changing how the project's money position is "
                + "read by everyone.",
            CommandType: typeof(SetCostCentreFinalisation),
            ResultType: typeof(CostCentreCostProgress),
            AuthorisationType: typeof(SetCostCentreFinalisationAuthorisation),
            ValidationType: typeof(SetCostCentreFinalisationValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before locking or unlocking a centre — it changes how "
                + "remaining funds are reported. costCode comes from list_cost_codes."),

        new AiAction(
            Name: "create_cost_centre_group",
            Area: "Commercial",
            Description: "Creates a named roll-up of two or more cost centres on the Financials tab so "
                + "related centres read as one line. Presentation only — no underlying money moves. "
                + "Rejected when a centre already sits in another group, unless that group is listed in "
                + "replaceGroupIds to be dissolved and absorbed in the same save.",
            CommandType: typeof(CreateCostCentreGroup),
            ResultType: typeof(CostCentreGroup),
            AuthorisationType: typeof(CreateCostCentreGroupAuthorisation),
            ValidationType: typeof(CreateCostCentreGroupValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; costCodes from list_cost_codes."),

        new AiAction(
            Name: "remove_cost_centre_group",
            Area: "Commercial",
            Description: "Dissolves a cost centre roll-up; its centres return to individual rows on the "
                + "Financials tab. Presentation only — nothing else is deleted and no money moves.",
            CommandType: typeof(RemoveCostCentreGroup),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(CreateCostCentreGroupAuthorisation),
            ValidationType: null,
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "costCentreGroupId comes from the project's cost centre groups list."),

        new AiAction(
            Name: "save_reconciliation_package",
            Area: "Commercial",
            Description: "Creates or wholly replaces a reconciliation package's definition — the tie "
                + "between work orders (cost side) and valuation sales lines or £ slices (sales side) "
                + "that the Financials tab reports profit per package from. Presentation only; nothing "
                + "writes to Xero. Locked packages cannot be edited.",
            CommandType: typeof(SaveReconciliationPackage),
            ResultType: typeof(ReconciliationPackage),
            AuthorisationType: typeof(ReconciliationPackageAuthorisation),
            ValidationType: typeof(SaveReconciliationPackageValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Null reconciliationPackageId creates; an existing id replaces that package's whole "
                + "definition — read the current definition first and carry forward what should not "
                + "change. Work order ids come from list_work_orders."),

        new AiAction(
            Name: "remove_reconciliation_package",
            Area: "Commercial",
            Description: "Dissolves a reconciliation package (it must be unlocked). Nothing underneath "
                + "is deleted and no money moves — the package is presentation only.",
            CommandType: typeof(RemoveReconciliationPackage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ReconciliationPackageAuthorisation),
            ValidationType: null,
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which package, by name, before calling."),

        new AiAction(
            Name: "set_reconciliation_package_lock",
            Area: "Commercial",
            Description: "Locks a reconciliation package — freezing its figures and realising profit or "
                + "loss against actual invoiced cost rather than committed orders — or unlocks it, "
                + "clearing the snapshot so the figures go live again.",
            CommandType: typeof(SetReconciliationPackageLock),
            ResultType: typeof(ReconciliationPackage),
            AuthorisationType: typeof(ReconciliationPackageAuthorisation),
            ValidationType: null,
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before locking or unlocking — locking banks profit/loss "
                + "figures on the Financials tab."),

        new AiAction(
            Name: "set_valuation_line_cost_centre",
            Area: "Commercial",
            Description: "Recodes which cost centre a valuation line's value sits against — a financial "
                + "correction that moves the line's value between cost centres without changing the "
                + "agreed amount. Exists so finance can correct allocation on variation lines frozen at "
                + "VO approval. The change is audited.",
            CommandType: typeof(SetValuationLineCostCentre),
            ResultType: typeof(ValuationLineItem),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(SetValuationLineCostCentreValidation),
            VisibleTo: CostCentreRecoders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "valuationLineItemId comes from the valuation report's lines; costCode from "
                + "list_cost_codes."),

        new AiAction(
            Name: "set_client_cost_references",
            Area: "Commercial",
            Description: "Replaces the project's WHOLE cost centre to client schedule-of-works "
                + "reference map in one save: entries with a reference are kept, blank references are "
                + "removed, and any cost centre not listed is removed too. Report setup — no amounts "
                + "change.",
            CommandType: typeof(SetClientCostReferences),
            ResultType: typeof(IReadOnlyList<ClientCostReference>),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(SetClientCostReferencesValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This is a full replace — read the current map first and include every entry that "
                + "should survive, or it will be removed."),

        new AiAction(
            Name: "set_xero_line_work_order_links",
            Area: "Commercial",
            Description: "Replaces the set of work-order links on an allocated Xero purchase line — "
                + "deciding which work orders that invoice money counts against (invoiced-to-date). One "
                + "full-net slice is the everyday whole-line link; several slices split a bill across "
                + "orders; an empty list clears all links. Slices may total less than the line (the "
                + "remainder counts as non-work-order cost of sales) but never more.",
            CommandType: typeof(SetXeroLineWorkOrderLinks),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetXeroLineWorkOrderLinksAuthorisation),
            ValidationType: typeof(SetXeroLineWorkOrderLinksValidation),
            VisibleTo: XeroWorkOrderLinkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This replaces ALL links on the line — include every slice that should remain. Work "
                + "order ids come from list_work_orders; no slice may take an order past its value."),

        // ── Commercial: timesheets ───────────────────────────────────────────────────────

        new AiAction(
            Name: "submit_timesheet",
            Area: "Commercial",
            Description: "Submits a timesheet — hours worked by a named person on a project against a "
                + "cost code. Pending until approved; only approved time becomes actual labour cost.",
            CommandType: typeof(SubmitTimesheet),
            ResultType: typeof(Timesheet),
            AuthorisationType: typeof(SubmitTimesheetAuthorisation),
            ValidationType: typeof(SubmitTimesheetValidation),
            VisibleTo: TimesheetSubmitters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "personEmail is the worker the time is for — pass it explicitly; it is not stamped "
                + "from the signed-in user. costCode comes from list_cost_codes."),

        new AiAction(
            Name: "approve_timesheet",
            Area: "Commercial",
            Description: "Approves a submitted timesheet, turning its hours into actual labour cost on "
                + "the project at the applicable rate.",
            CommandType: typeof(ApproveTimesheet),
            ResultType: typeof(Timesheet),
            AuthorisationType: typeof(ApproveTimesheetAuthorisation),
            ValidationType: typeof(ApproveTimesheetValidation),
            VisibleTo: TimesheetApprovers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — approval is a cost-committing step. "
                + "timesheetId comes from the project's timesheets list."),

        // ── Commercial inputs: dayworks, contra charges, subcontractor retentions ────────

        new AiAction(
            Name: "log_daywork",
            Area: "Commercial",
            Description: "Logs a daywork on a project — labour, plant and materials costs with uplift, "
                + "producing the chargeable amount recorded against the subcontractor reference. Adds a "
                + "cost/recovery record the commercial team reports from.",
            CommandType: typeof(LogDaywork),
            ResultType: typeof(Daywork),
            AuthorisationType: typeof(LogDayworkAuthorisation),
            ValidationType: typeof(LogDayworkValidation),
            VisibleTo: DayworkLoggers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_contra_charge",
            Area: "Commercial",
            Description: "Records a contra charge against a subcontractor — an amount to be recovered "
                + "from them (with category, status and recovered-to-date) that the commercial team "
                + "offsets against what the subcontractor is owed.",
            CommandType: typeof(RecordContraCharge),
            ResultType: typeof(ContraCharge),
            AuthorisationType: typeof(RecordContraChargeAuthorisation),
            ValidationType: typeof(RecordContraChargeValidation),
            VisibleTo: ContraChargeRecorders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_subcontractor_retention",
            Area: "Commercial",
            Description: "Records a subcontractor's retention position on a project — certified amount, "
                + "retention percent and the first/final released amounts — the money held back from "
                + "the subcontractor and what has been released.",
            CommandType: typeof(RecordSubcontractorRetention),
            ResultType: typeof(SubcontractorRetention),
            AuthorisationType: typeof(RecordSubcontractorRetentionAuthorisation),
            ValidationType: typeof(RecordSubcontractorRetentionValidation),
            VisibleTo: RetentionRecorders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        // ── CVR ──────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "capture_cvr_snapshot",
            Area: "CVR",
            Description: "Captures a CVR (cost value reconciliation) snapshot for a project — tender "
                + "value, forecast final cost, forecast final value and weeks ahead/behind — the "
                + "period's recorded view of forecast profit.",
            CommandType: typeof(CaptureCvrSnapshot),
            ResultType: typeof(CvrSnapshot),
            AuthorisationType: typeof(CaptureCvrSnapshotAuthorisation),
            ValidationType: typeof(CaptureCvrSnapshotValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the figures with the user before calling — a snapshot is a period record. "
                + "projectId comes from list_projects."),

        new AiAction(
            Name: "record_cvr_package_row",
            Area: "CVR",
            Description: "Records a package row on a project's CVR — order cost/value and variation "
                + "cost/value for one named package, feeding the CVR's cost-versus-value position.",
            CommandType: typeof(RecordCvrPackageRow),
            ResultType: typeof(CvrPackageRow),
            AuthorisationType: typeof(RecordCvrPackageRowAuthorisation),
            ValidationType: typeof(RecordCvrPackageRowValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_forecast_component",
            Area: "CVR",
            Description: "Records a cost-forecast component for one package on a project's CVR — cost "
                + "incurred, committed, QS accrual, prelim forecast and cost to complete — the build-up "
                + "behind the forecast final cost.",
            CommandType: typeof(RecordForecastComponent),
            ResultType: typeof(ForecastComponent),
            AuthorisationType: typeof(RecordForecastComponentAuthorisation),
            ValidationType: typeof(RecordForecastComponentValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_prelim_forecast_for_week",
            Area: "CVR",
            Description: "Records one week's prelim position for a prelim item on a project — tendered, "
                + "actual and forecast amounts — feeding the prelims run-rate in the CVR.",
            CommandType: typeof(RecordPrelimForecastForWeek),
            ResultType: typeof(PrelimForecastEntry),
            AuthorisationType: typeof(RecordPrelimForecastForWeekAuthorisation),
            ValidationType: typeof(RecordPrelimForecastForWeekValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_qs_accrual",
            Area: "CVR",
            Description: "Records a QS accrual on a project — add/omit amounts and the liability "
                + "carried in the CVR for cost known but not yet invoiced, signed off by a named "
                + "person.",
            CommandType: typeof(RecordQsAccrual),
            ResultType: typeof(QsAccrual),
            AuthorisationType: typeof(RecordQsAccrualAuthorisation),
            ValidationType: typeof(RecordQsAccrualValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "signedOffByEmail is the accountable signer's portal email — pass it explicitly; it "
                + "is not stamped from the signed-in user. projectId comes from list_projects."),

        new AiAction(
            Name: "update_qs_accrual",
            Area: "CVR",
            Description: "Rewrites an existing QS accrual's details — category, description, add/omit "
                + "amounts, liability and signer — changing the accrued liability the CVR carries.",
            CommandType: typeof(UpdateQsAccrual),
            ResultType: typeof(QsAccrual),
            AuthorisationType: typeof(UpdateQsAccrualAuthorisation),
            ValidationType: typeof(UpdateQsAccrualValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This replaces every field on the accrual — read the current record first and carry "
                + "forward what should not change. qsAccrualId comes from the project's QS accruals "
                + "list."),

        new AiAction(
            Name: "grant_eot",
            Area: "CVR",
            Description: "Grants an extension of time (EOT) on a project — days granted with the "
                + "commercial recovery amount attached. A contractual/commercial commitment; directors "
                + "only.",
            CommandType: typeof(GrantEot),
            ResultType: typeof(Eot),
            AuthorisationType: typeof(GrantEotAuthorisation),
            ValidationType: typeof(GrantEotValidation),
            VisibleTo: DirectorsOnly,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — granting an EOT is a formal commitment. "
                + "projectId comes from list_projects."),

        new AiAction(
            Name: "update_eot",
            Area: "CVR",
            Description: "Rewrites a granted EOT's reason, days granted and commercial recovery amount "
                + "— changing a formal commitment already on record. Directors only.",
            CommandType: typeof(UpdateEot),
            ResultType: typeof(Eot),
            AuthorisationType: typeof(UpdateEotAuthorisation),
            ValidationType: typeof(UpdateEotValidation),
            VisibleTo: DirectorsOnly,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. eotId comes from the project's EOTs list; "
                + "this replaces all three fields, so carry forward what should not change."),

        // ── Cashflow ─────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "capture_cashflow_snapshot",
            Area: "Cashflow",
            Description: "Captures a company-wide 13-week cashflow snapshot — expected income and "
                + "committed spend — recording the net cash position the directors report from. Not "
                + "per-project.",
            CommandType: typeof(CaptureCashflowSnapshot),
            ResultType: typeof(CashflowSnapshot),
            AuthorisationType: typeof(CaptureCashflowSnapshotAuthorisation),
            ValidationType: typeof(CaptureCashflowSnapshotValidation),
            VisibleTo: CashflowCapturers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the figures with the user before calling — a snapshot is a standing "
                + "financial record.")
    };

    // No skipped endpoints: every command endpoint under Features/Commercial,
    // Features/CommercialInputs, Features/Cvr and Features/Cashflow dispatches an
    // ICommandHandler with JSON-body or route-parameter binding, and none are already
    // exposed by AiWriteTools.
}
