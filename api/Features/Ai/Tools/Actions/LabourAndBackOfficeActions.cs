using Jewel.JPMS.Api.Features.AccessRequests.Commands;
using Jewel.JPMS.Api.Features.Ai.Skills;
using Jewel.JPMS.Api.Features.CostCenters.Commands;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Platform.Commands;
using Jewel.JPMS.Api.Features.Rates.Commands;
using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Platform;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Labour and back-office commands as connector actions. Mirrors the command endpoints
/// under Features/Labour, Features/CostCenters, Features/Rates, Features/Hs,
/// Features/UsefulInformation, Features/Platform and Features/AccessRequests. Most Labour, Xero
/// and Registers command endpoints still gate with inline role checks and have no Authorisation
/// classes, so they cannot be mirrored here — each is recorded in the skip list at the bottom of
/// this file. The Labour exceptions (2026-08-28): AddWorker now has real gate classes its
/// endpoint composes, and the by-name connector-shaped commands SubmitWorkerWeekByName and
/// RecordWorkerAbsenceByName carry their own gates and resolve workers by NAME — built because
/// the only timesheet entry the connector once had was the legacy Commercial SubmitTimesheet
/// (slices deleted 2026-08-28), whose email-and-cost-code schema taught models to demand data
/// the portal does not need.
/// Where an authorisation keeps its role set as a private field, the VisibleTo below replicates
/// the identical roles with RoleSet.Of(...) and a comment names the source; where the set is an
/// accessible internal static (UsefulInformationRoles), it is referenced directly.</summary>
internal sealed class LabourAndBackOfficeActions : IAiActionSource
{
    // Replica of AddCostCenterAuthorisation.RolesThatMayManageCostCenters (identical set in
    // ReviseCostCenterAuthorisation).
    private static readonly RoleSet CostCenterManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    // Replica of AddRateAuthorisation.RolesThatMayEditRates (identical to
    // ReviseRateAuthorisation.RolesThatMayReviseRates).
    private static readonly RoleSet RateEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    // Replica of LogHsRecordAuthorisation.RolesThatMayLogHsRecords (identical sets in
    // UpdateHsRecordAuthorisation and RecordAttendanceForHsRecordAuthorisation).
    private static readonly RoleSet HsRecordManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead);

    // Mirror of AdminGate.Allows — Role.Admin or the Finance Director, who is granted the same
    // permissions without holding the Admin identity (see AdminGate).
    private static readonly RoleSet AdminGateRoles =
        RoleSet.Of(Role.Admin, JpmsRoles.FinanceDirector);

    // SubmitAccessRequestAuthorisation checks no roles at all — only that the command's Email is
    // the signed-in user's own email — so the broadest possible set is every portal role.
    private static readonly RoleSet AnySignedInRole =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
            JpmsRoles.OfficeAdmin, JpmsRoles.Architect, JpmsRoles.Client,
            JpmsRoles.Subcontractor, JpmsRoles.Foreman, JpmsRoles.SiteOperative,
            JpmsRoles.Accounts);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ---- Labour -------------------------------------------------------------------------

        new AiAction(
            Name: "add_worker",
            Area: "Labour",
            Description: "Adds a worker to the company-wide worker register with their hourly "
                + "rate — the register that week entry, approvals and the Labour overview all key "
                + "on. Creates no portal account and sends nothing; the worker simply becomes "
                + "available to log time and absences against. The rate is money-facing: approved "
                + "hours are costed at it.",
            CommandType: typeof(AddWorker),
            ResultType: typeof(Worker),
            AuthorisationType: typeof(AddWorkerAuthorisation),
            ValidationType: typeof(AddWorkerValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Only name and hourlyRate are needed — NEVER ask the user for a worker's email "
                + "or id. contactEmail exists solely to link the worker's own portal sign-in "
                + "later and is normally left out; contactPhone likewise. subcontractorId links "
                + "the worker to their subcontractor company where the user names one — omit it "
                + "rather than guessing. A worker added by mistake can be deactivated on the "
                + "Workers page."),

        new AiAction(
            Name: "record_worker_absence",
            Area: "Labour",
            Description: "Records one worker's absence on one date — Holiday, HalfDay, NotWorked "
                + "or Sick — visible at once on the company-wide Labour overview and its "
                + "forecast. One absence per worker per date: recording the same day again "
                + "replaces the kind and note. Week entry (submit_worker_week) skips days already "
                + "covered by an absence.",
            CommandType: typeof(RecordWorkerAbsenceByName),
            ResultType: typeof(WorkerAbsence),
            AuthorisationType: typeof(RecordWorkerAbsenceByNameAuthorisation),
            ValidationType: typeof(RecordWorkerAbsenceByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: new[] { "RecordedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "workerName is the worker's name as the user says it, matched server-side "
                + "against the worker register — NEVER ask the user for worker emails or ids; "
                + "names are how workers are identified. date is the single day the absence "
                + "covers; a run of days off is one call per day. kind is Holiday, HalfDay, "
                + "NotWorked or Sick; note is optional."),

        new AiAction(
            Name: "submit_worker_week",
            Area: "Labour",
            Description: "Enters one worker's week of site attendance — which site (project) they "
                + "were on each day and for how many hours — the connector's equivalent of the "
                + "Labour overview's Enter-a-week form. Each day lands as a Submitted timesheet "
                + "in that project's approval queue; only approved time becomes actual labour "
                + "cost. Days already carrying a timesheet or a recorded absence are skipped, "
                + "never overwritten, and the per-day outcomes say exactly what happened.",
            CommandType: typeof(SubmitWorkerWeekByName),
            ResultType: typeof(WorkerWeekResult),
            AuthorisationType: typeof(SubmitWorkerWeekByNameAuthorisation),
            ValidationType: typeof(SubmitWorkerWeekByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName is the worker's name as the user says it, matched server-side "
                + "against the worker register — NEVER ask the user for worker emails or ids; "
                + "names are how timesheets identify people. weekStart is the Monday. Each day "
                + "needs a projectId from list_projects. costCode is OPTIONAL and normally left "
                + "out: the person entering records WHERE people were, the approver codes each "
                + "day at approval, and an uncoded day cannot be approved until coded — so "
                + "leaving it blank enforces the coding step rather than skipping it. A day "
                + "split across two sites is two entries with the same date, one per site, with "
                + "the hours split."),

        new AiAction(
            Name: "code_worker_week",
            Area: "Labour",
            Description: "Applies ONE cost code to a worker's Submitted timesheets in a week on "
                + "one project — the Labour tab's bulk coding, by name. Coding is the step before "
                + "approval: an uncoded day cannot be approved. Runs the grid's own Adjust per "
                + "row (hours unchanged); rows already approved are immutable and report so "
                + "rather than change. Per-day outcomes say exactly what was coded and what "
                + "was not.",
            CommandType: typeof(CodeWorkerWeekByName),
            ResultType: typeof(WorkerWeekCodingResult),
            AuthorisationType: typeof(CodeWorkerWeekByNameAuthorisation),
            ValidationType: typeof(CodeWorkerWeekByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it, matched against the worker register — NEVER "
                + "ask for worker emails or ids. projectId from list_projects; weekStart is the "
                + "Monday. costCode must be a Code from list_cost_codes, spelled exactly. dates "
                + "narrows the act to specific days (ISO dates within the week); leave it out to "
                + "code the worker's whole week on that project. View the week first with "
                + "view_labour_week so the user is coding what they think they are."),

        new AiAction(
            Name: "approve_worker_week",
            Area: "Labour",
            Description: "Approves a worker's Submitted timesheets in a week on one project — the "
                + "Labour tab's Approve selected, by name. Approval POSTS the hours to Financials "
                + "as actual labour cost at the worker's rate, and an approved timesheet is "
                + "immutable — its cost code and hours can never be changed afterwards (the "
                + "correction path is reject-and-resubmit). Uncoded days are refused until coded "
                + "(code_worker_week); the per-cost-code budget hard-block applies, and a "
                + "budget refusal reports the code's current allocated/spent/committed figures. "
                + "MD/FD/Admin may deliberately approve PAST the block with allowOverBudget: true "
                + "plus a typed overBudgetReason — audited per day, like the Labour tab's own "
                + "override. Partial "
                + "success: per-day outcomes report what approved and what was refused, and why.",
            CommandType: typeof(ApproveWorkerWeekByName),
            ResultType: typeof(WorkerWeekApprovalResult),
            AuthorisationType: typeof(ApproveWorkerWeekByNameAuthorisation),
            ValidationType: typeof(ApproveWorkerWeekByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: new[] { "ApprovedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Show the user the days about to be approved (view_labour_week) and get their "
                + "yes first — approval is final. workerName as the user says it; projectId from "
                + "list_projects; weekStart is the Monday. dates narrows approval to specific "
                + "days; leave it out to approve every Submitted day of the worker's week on "
                + "that project. allowOverBudget is never a default: offer it only after a budget "
                + "refusal, only for the MD/FD/Admin, and put the block you are overriding and "
                + "the user's own reason in front of them in the confirm turn — the alternatives "
                + "(re-code, or re-allocate via set_cost_code_budget) may be the better answer."),

        new AiAction(
            Name: "reject_worker_day",
            Area: "Labour",
            Description: "Rejects a worker's Submitted timesheet on one date back to them with a "
                + "reason — the Labour tab's Reject, by name. The worker reads the reason on "
                + "their My day page and can resubmit; nothing is deleted. Approved timesheets "
                + "are immutable and refuse.",
            CommandType: typeof(RejectWorkerDayByName),
            ResultType: typeof(TimesheetDetail),
            AuthorisationType: typeof(RejectWorkerDayByNameAuthorisation),
            ValidationType: typeof(RejectWorkerDayByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; projectId from list_projects; date is the "
                + "single day being rejected. reason is mandatory and the worker sees it — write "
                + "it to them (\"Hours look double-entered — please re-check Tuesday\")."),

        // ---- Labour month-end: sign-off, the Xero coding run, reconciliation, mappings -------
        // (2026-08-31, the accountant's ask: the whole month-end chain from the connector —
        // sign off → run the coding → human approves the bill in Xero → mark the cover /
        // post the variance. view_settlement_month is the read that pairs with these.)

        new AiAction(
            Name: "sign_off_labour_week",
            Area: "Labour",
            Description: "Places the weekly sign-off marker on a worker's week — the Labour "
                + "overview's Sign off, by name. Sign-off freezes the week for settlement: only "
                + "fully signed-off worker-months are written to Xero by run_xero_coding. The "
                + "server re-checks the signable rule at the moment of signing — every elapsed "
                + "day must be approved, rejected or recorded as absence — and refuses with the "
                + "reason when it fails. Touches no timesheet; sign-off is a marker over "
                + "approval, never a second state machine.",
            CommandType: typeof(SignOffWorkerWeekByName),
            ResultType: typeof(LabourWeekSignOff),
            AuthorisationType: typeof(SignOffWorkerWeekByNameAuthorisation),
            ValidationType: typeof(SignOffWorkerWeekByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: new[] { "SignedOffByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "workerName as the user says it; weekStart is any date in the week wanted "
                + "(normalised to that week's Monday). Show the user the week first "
                + "(view_labour_week) and get their yes — signing off is what arms the Xero "
                + "coding run for that week. A refusal names the unsettled days: approve or "
                + "reject them, or record a genuine absence (record_worker_absence), then sign "
                + "again. remove_labour_week_sign_off is the undo."),

        new AiAction(
            Name: "remove_labour_week_sign_off",
            Area: "Labour",
            Description: "Removes the weekly sign-off marker from a worker's week — the undo of "
                + "sign_off_labour_week. Touches no timesheet; the week simply drops back out of "
                + "settlement scope until it is signed off again.",
            CommandType: typeof(RemoveWorkerWeekSignOffByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveWorkerWeekSignOffByNameAuthorisation),
            ValidationType: typeof(RemoveWorkerWeekSignOffByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; weekStart is any date in the week. A "
                + "worker-month the coding run has ALREADY written stays written — removing a "
                + "sign-off never un-codes a bill; re-running after a schedule change is a "
                + "deliberate human decision taken on the settlement view."),

        new AiAction(
            Name: "run_xero_coding",
            Area: "Labour",
            Description: "WRITES TO XERO: runs the month's automated labour coding — the Labour "
                + "overview's Run Xero coding. For each fully signed-off worker-month it recodes "
                + "the covered Dext draft bill to the settlement schedule's split (Sites and Cost "
                + "Code tracking per the effective-dated mappings) or stages a draft bill where "
                + "none has arrived. Everything lands DRAFT in Xero — approving the bill there "
                + "stays human. Unsigned weeks, mapping gaps, open variances and already-coded "
                + "months skip-and-report; the run never guesses a code and never writes from "
                + "unsigned data. Returns per-worker outcomes the way approval outcomes come "
                + "back: BillRecoded, DraftStaged, Skipped or Failed, each with the detail in "
                + "the run's own words.",
            CommandType: typeof(RunXeroCodingByName),
            ResultType: typeof(XeroCodingRunReport),
            AuthorisationType: typeof(RunXeroCodingByNameAuthorisation),
            ValidationType: typeof(RunXeroCodingByNameValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "RunByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Read view_settlement_month FIRST and show the user who will code and who "
                + "will skip (FullySignedOff, verdict, lastCodingOutcome tell you) before the "
                + "confirm turn. workerNames narrows the run to named workers; leave it out to "
                + "run everyone with activity in the month. Every skip's detail names its fix: "
                + "not signed off → sign_off_labour_week; a mapping gap → set_site_xero_mapping "
                + "or set_cost_code_xero_mapping, then re-run; an open variance → resolve it on "
                + "the settlement view first. Already-coded worker-months skip by design "
                + "(run-once) — relay that rather than trying to force a re-run."),

        new AiAction(
            Name: "set_xero_line_timesheet_cover",
            Area: "Labour",
            Description: "Marks (or with isCovered: false unmarks) a Xero purchase line as "
                + "settlement of approved timesheets — the reconciliation tick once the "
                + "authorised bill syncs back. Covered lines are excluded from the cost-of-sales "
                + "aggregation so labour is never double-counted: the approved timesheet is the "
                + "actual, the invoice is settlement of it.",
            CommandType: typeof(SetXeroLineTimesheetCover),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetXeroLineTimesheetCoverAuthorisation),
            ValidationType: typeof(SetXeroLineTimesheetCoverValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "xeroLedgerLineId comes from list_xero_ledger_lines; projectId from "
                + "list_projects; subcontractorId is the worker's linked subcontractor company "
                + "(view_settlement_month carries it); periodStart/periodEnd bound the month the "
                + "line settles. Tell the user plainly which line you marked against which "
                + "worker-month — the marking moves reported cost of sales, and isCovered: false "
                + "is the undo if a line was marked in error."),

        new AiAction(
            Name: "add_labour_settlement_variance",
            Area: "Labour",
            Description: "Posts an accepted invoice-vs-timesheet difference as a visible "
                + "settlement variance against a cost code on a project, so posted cost of sales "
                + "equals cash paid and nothing is silently absorbed. This is resolution path (4) "
                + "when a covered bill's total will not tie to the schedule: real money posts "
                + "against the code and there is no remove — a wrong variance is corrected by "
                + "posting its opposite, visibly.",
            CommandType: typeof(AddLabourSettlementVariance),
            ResultType: typeof(LabourSettlementVariance),
            AuthorisationType: typeof(AddLabourSettlementVarianceAuthorisation),
            ValidationType: typeof(AddLabourSettlementVarianceValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "projectId from list_projects; costCode from list_cost_codes; amount is the "
                + "signed difference being accepted (positive = paying more than the timesheets "
                + "say); reason is mandatory and shows on the settlement view; xeroLedgerLineId "
                + "ties it to the bill line when there is one. In the confirm turn put the "
                + "schedule total, the bill total and the difference side by side — re-coding or "
                + "chasing a corrected invoice may be the honest route instead."),

        new AiAction(
            Name: "set_site_xero_mapping",
            Area: "Labour",
            Description: "Points a project at a Xero site tracking option from now on — the "
                + "mapping the coding run uses for the Sites tracking on every schedule line. "
                + "Effective-dated close-and-replace: the open row (if any) is closed with "
                + "EffectiveTo = now, never edited, so historic reads keep translating through "
                + "it; a mid-month re-map codes the month the new way.",
            CommandType: typeof(SetSiteXeroMapping),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetSiteXeroMappingAuthorisation),
            ValidationType: typeof(SetSiteXeroMappingValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "projectId from list_projects; xeroTrackingOptionName spelled EXACTLY as Xero "
                + "holds the option — the coding run matches by this name and a misspelling just "
                + "moves the skip. xeroTrackingOptionId is optional — leave it out unless the "
                + "user gives you Xero's own option id. get_xero_mappings shows the current rows; "
                + "in the confirm turn show the user the old mapping (or \"unmapped\") next to "
                + "the new one."),

        new AiAction(
            Name: "set_cost_code_xero_mapping",
            Area: "Labour",
            Description: "Sets a cost code's Xero mapping from now on: its Cost Code tracking "
                + "option and the account code per line nature (labour / materials / travel) the "
                + "coding run posts to. Same effective-dated close-and-replace contract as "
                + "set_site_xero_mapping — history is never edited.",
            CommandType: typeof(SetCostCodeXeroMapping),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetCostCodeXeroMappingAuthorisation),
            ValidationType: typeof(SetCostCodeXeroMappingValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "costCode from list_cost_codes. A blank account code for a nature the "
                + "worker's lines use makes the run SKIP that worker (\"no CisLabour account "
                + "code\") — when fixing a gap the run reported, set the account code for every "
                + "nature in play. get_xero_mappings shows the current rows; show old → new in "
                + "the confirm turn. xeroTrackingOptionName may stay blank — the run then uses "
                + "the cost code itself as the tracking option name."),

        // ---- Cost centres -------------------------------------------------------------------

        new AiAction(
            Name: "add_cost_center",
            Area: "Cost centres",
            Description: "Adds a cost code to the GLOBAL cost-center master — it appears at once in "
                + "the cost-code dropdowns and the Financials views that every project's money is "
                + "coded against. This is a commercial control shared by all projects, not a "
                + "per-project setting.",
            CommandType: typeof(AddCostCenter),
            ResultType: typeof(CostCenter),
            AuthorisationType: typeof(AddCostCenterAuthorisation),
            ValidationType: typeof(AddCostCenterValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Pass sortOrder 0 to append after the current last code. Duplicate codes are "
                + "refused by the handler."),

        new AiAction(
            Name: "revise_cost_center",
            Area: "Cost centres",
            Description: "Revises a cost code in the global cost-center master — code, name, order "
                + "and active flag — changing how money is coded on every project from now on. "
                + "Setting isActive false retires the code: it drops out of dropdowns and the "
                + "Financials view without deleting it, so historical allocations keep resolving.",
            CommandType: typeof(ReviseCostCenter),
            ResultType: typeof(CostCenter),
            AuthorisationType: typeof(ReviseCostCenterAuthorisation),
            ValidationType: typeof(ReviseCostCenterValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "costCenterId identifies the existing code (over HTTP it is the route value). "
                + "Confirm with the user before retiring a code — it disappears from every "
                + "project's dropdowns at once."),

        // ---- Rates --------------------------------------------------------------------------

        new AiAction(
            Name: "add_rate",
            Area: "Rates",
            Description: "Adds a rate to the company rate library (trade, description, unit, £ "
                + "value, supplier) — the priced reference the commercial team estimates and "
                + "prices work from. Money-facing: a wrong value here feeds wrong pricing.",
            CommandType: typeof(AddRate),
            ResultType: typeof(Rate),
            AuthorisationType: typeof(AddRateAuthorisation),
            ValidationType: typeof(AddRateValidation),
            VisibleTo: RateEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "revise_rate",
            Area: "Rates",
            Description: "Revises an existing rate in the company rate library, replacing its "
                + "trade, description, unit, £ value and supplier in one write. Money-facing: the "
                + "revised value is what future pricing reads.",
            CommandType: typeof(ReviseRate),
            ResultType: typeof(Rate),
            AuthorisationType: typeof(ReviseRateAuthorisation),
            ValidationType: typeof(ReviseRateValidation),
            VisibleTo: RateEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "rateId identifies the existing rate. All fields are replaced — carry forward "
                + "the values that should not change."),

        // ---- Health & safety ----------------------------------------------------------------

        new AiAction(
            Name: "log_hs_record",
            Area: "Health & safety",
            Description: "Logs a health & safety record on a project — an observation, near miss, "
                + "incident, corrective action, toolbox talk or permit — visible on the project's "
                + "H&S register immediately and assigned to a named person by email.",
            CommandType: typeof(LogHsRecord),
            ResultType: typeof(HsRecord),
            AuthorisationType: typeof(LogHsRecordAuthorisation),
            ValidationType: typeof(LogHsRecordValidation),
            VisibleTo: HsRecordManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. kind is one of Observation, NearMiss, "
                + "Incident, CorrectiveAction, ToolboxTalk, Permit; severity is Low, Medium, High "
                + "or Critical. assignedToEmail is the assignee's portal email."),

        new AiAction(
            Name: "update_hs_record",
            Area: "Health & safety",
            Description: "Updates an existing health & safety record — summary, severity, status "
                + "(Open, InProgress, Closed), assignee and due date. Setting status Closed closes "
                + "the record on the project's H&S register.",
            CommandType: typeof(UpdateHsRecord),
            ResultType: typeof(HsRecord),
            AuthorisationType: typeof(UpdateHsRecordAuthorisation),
            ValidationType: typeof(UpdateHsRecordValidation),
            VisibleTo: HsRecordManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "hsRecordId identifies the record. All listed fields are replaced — carry "
                + "forward what should not change."),

        new AiAction(
            Name: "record_attendance_for_hs_record",
            Area: "Health & safety",
            Description: "Records a named attendee against a health & safety record (typically a "
                + "toolbox talk register) — the attendance row is on the record immediately.",
            CommandType: typeof(RecordAttendanceForHsRecord),
            ResultType: typeof(HsRecordAttendance),
            AuthorisationType: typeof(RecordAttendanceForHsRecordAuthorisation),
            ValidationType: typeof(RecordAttendanceForHsRecordValidation),
            VisibleTo: HsRecordManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "signatureBlobRef is a reference to a captured signature blob — normally taken "
                + "on-site through the portal UI; only hsRecordId and attendeeName are required."),

        // ---- Useful information -------------------------------------------------------------

        new AiAction(
            Name: "add_useful_information_note",
            Area: "Useful information",
            Description: "Adds a Useful Information note to a project — internal reference "
                + "material such as door codes, key safe locations and site access notes, visible "
                + "to all staff on the project's Useful Information tab immediately. Never shown to "
                + "external logins. Recorded as created by the signed-in user.",
            CommandType: typeof(AddUsefulInformationNote),
            ResultType: typeof(UsefulInformationNote),
            AuthorisationType: typeof(AddUsefulInformationNoteAuthorisation),
            ValidationType: typeof(AddUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "update_useful_information_note",
            Area: "Useful information",
            Description: "Replaces a Useful Information note's title and body in one write — the "
                + "whole staff sees the new text immediately. Recorded as edited by the signed-in "
                + "user.",
            CommandType: typeof(UpdateUsefulInformationNote),
            ResultType: typeof(UsefulInformationNote),
            AuthorisationType: typeof(UpdateUsefulInformationNoteAuthorisation),
            ValidationType: typeof(UpdateUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: new[] { "UpdatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "usefulInformationNoteId identifies the note. Both title and body are replaced "
                + "— read the current note first and carry forward what should not change."),

        new AiAction(
            Name: "delete_useful_information_note",
            Area: "Useful information",
            Description: "Deletes a Useful Information note permanently. There is no undo.",
            CommandType: typeof(DeleteUsefulInformationNote),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteUsefulInformationNoteAuthorisation),
            ValidationType: typeof(DeleteUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which note, by title, before calling."),

        // ---- Platform -----------------------------------------------------------------------

        new AiAction(
            Name: "publish_app_version",
            Area: "Platform",
            Description: "Bumps the announced app version by one, which raises the update toast on "
                + "EVERY open portal tab and prompts every signed-in user to refresh. Carries no "
                + "target number — one call, one increment, no way to move the number backwards.",
            CommandType: typeof(PublishAppVersion),
            ResultType: typeof(AnnouncedAppVersion),
            AuthorisationType: typeof(PublishAppVersionAuthorisation),
            ValidationType: typeof(PublishAppVersionValidation),
            VisibleTo: AdminGateRoles,
            EmailStamps: new[] { "PublishedBy" },
            NameStamps: Array.Empty<string>(),
            Notes: "Affects every user's open session at once and cannot be undone — confirm with "
                + "the user before calling."),

        new AiAction(
            Name: "attach_action_skills",
            Area: "Platform",
            Description: "Replaces the set of skills attached to one connector action or to a whole "
                + "action area — the wiring the AI Actions admin page edits. An attached skill's "
                + "doctrine is served by describe_action with that action's contract from the very "
                + "next call. An empty skill list detaches everything from the target.",
            CommandType: typeof(SaveAiActionSkills),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SaveAiActionSkillsAuthorisation),
            ValidationType: typeof(SaveAiActionSkillsValidation),
            VisibleTo: SkillRoles.ManageSkills,
            EmailStamps: new[] { "SavedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "targetKind is \"action\" or \"area\"; targetKey is the action name or the area "
                + "exactly as list_actions shows it; skillKeys come from list_skills. The save "
                + "REPLACES the target's whole set, so include every skill that should remain "
                + "attached, not just the one being added."),

        // ---- Access requests ----------------------------------------------------------------

        new AiAction(
            Name: "submit_access_request",
            Area: "Access requests",
            Description: "Submits (or refreshes) a pending portal access request for the signed-in "
                + "user's own email — it appears on the administrators' pending access requests "
                + "list. Calling again for the same email updates the display name and request "
                + "time rather than creating a duplicate.",
            CommandType: typeof(SubmitAccessRequest),
            ResultType: typeof(AccessRequest),
            AuthorisationType: typeof(SubmitAccessRequestAuthorisation),
            ValidationType: typeof(SubmitAccessRequestValidation),
            VisibleTo: AnySignedInRole,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "email must be the signed-in user's own email — the authorisation rejects any "
                + "other value. Further per-record checks apply at execution."),

        new AiAction(
            Name: "resolve_access_request",
            Area: "Access requests",
            Description: "Resolves a pending access request by DELETING its row permanently — the "
                + "request disappears from the pending list and there is no undo. This does not "
                + "itself grant or deny access; it only clears the request.",
            CommandType: typeof(ResolveAccessRequest),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ResolveAccessRequestAuthorisation),
            ValidationType: typeof(ResolveAccessRequestValidation),
            VisibleTo: AdminGateRoles,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "email is the requester's email as listed by the pending access requests view. "
                + "Irreversible — confirm with the user before calling."),
    };

    // (AddLabourSettlementVariance is no longer skipped — gate classes added 2026-08-31 (SettlementCommandGates.cs), action add_labour_settlement_variance above; the command gained a CreatedByEmail stamp parameter the interface HandleAsync now carries, so the gateway path stamps the same actor the endpoint's overload does.)
    // (AddWorker is no longer skipped — gate classes added 2026-08-28, action add_worker above.)
    // Skipped: UpdateWorker — gate classes exist (2026-08-28, endpoint composes them), but the command is keyed by an opaque WorkerId the connector cannot resolve; expose via a by-name wrapper (the SubmitWorkerWeekByName pattern) if a need appears.
    // Skipped: DeleteWorker — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: AddWorkerTimesheet — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only).
    // (AdjustTimesheet is no longer connector-unreachable — the connector codes through CodeWorkerWeekByName (code_worker_week above), which resolves worker name + dates to the week's timesheets and runs AdjustTimesheetHandler per row. The id-keyed AdjustTimesheet itself stays unmirrored: opaque TimesheetId.)
    // (ApproveTimesheets likewise — the connector approves through ApproveWorkerWeekByName (approve_worker_week above, confirm-first), which delegates the resolved ids to ApproveTimesheetsHandler's approvedByEmail overload via the EmailStamps parameter. The id-keyed command stays unmirrored: no Authorisation class, opaque ids. Distinct from the legacy Commercial ApproveTimesheet, whose slices were deleted 2026-08-28.)
    // (RejectTimesheet likewise — reject_worker_day above resolves name + date and runs RejectTimesheetHandler. The id-keyed command stays unmirrored: no Authorisation class, opaque TimesheetId.)
    // Skipped: SubmitWorkerWeek — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only); the connector enters weeks through SubmitWorkerWeekByName (submit_worker_week above), which delegates to the same handler. (Distinct from the legacy Commercial SubmitTimesheet — action removed from CommercialActions and slices deleted, both 2026-08-28.)
    // Skipped: MySiteSignIn — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: MySiteSignOut — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: MyResubmitTimesheet — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // (SignOffLabourWeek is no longer connector-unreachable — the connector signs off through SignOffWorkerWeekByName (sign_off_labour_week above, confirm-first), which resolves the worker name and delegates to SignOffLabourWeekHandler's signedOffByEmail overload via the EmailStamps parameter, converting WeekNotSignableException to the gateway's message convention. The id-keyed SignOffLabourWeek itself stays unmirrored: opaque WorkerId.)
    // (RemoveLabourWeekSignOff likewise — remove_labour_week_sign_off above resolves the name and delegates to the registered handler. The id-keyed command stays unmirrored: opaque WorkerId.)
    // Skipped: SetProjectWorkerAssignment — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: SetWorkerContract — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: SetWorkerCisStatus — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // (RecordWorkerAbsence is no longer skipped — the connector records absences through RecordWorkerAbsenceByName (record_worker_absence above), which delegates to the same handler with the recordedByEmail overload's stamp carried as an EmailStamps parameter; the endpoint gained RecordWorkerAbsenceAuthorisation 2026-08-28.)
    // Skipped: RemoveWorkerAbsence — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: AddWorkerSettlementLine — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: RemoveWorkerSettlementLine — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // (SetSiteXeroMapping is no longer skipped — gate classes added 2026-08-31 (SettlementCommandGates.cs), action set_site_xero_mapping above.)
    // (SetCostCodeXeroMapping is no longer skipped — gate classes added 2026-08-31, action set_cost_code_xero_mapping above.)
    // (SetXeroLineTimesheetCover is no longer skipped — gate classes added 2026-08-31, action set_xero_line_timesheet_cover above; CreatedByEmail stamp parameter added, interface HandleAsync carries it.)
    // (RunXeroCoding is no longer connector-unreachable — run_xero_coding above delegates through RunXeroCodingByName, which resolves worker names and passes the caller into RunXeroCodingHandler's runByEmail overload. The id-keyed command stays unmirrored: opaque WorkerIds.)
    // Skipped: SyncXeroLedger — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: AllocateSuggestedXeroLines — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: RetryXeroWriteBack — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: SetXeroAllocation — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: SyncXeroSitePnl — no Authorisation class (inline XeroSitePnlRoles.AllowedToView check only).
    // Skipped: SaveRegisterItem — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: DeactivateRegisterItem — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: PublishPolicyDocument — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: SignPolicy — no Authorisation class, and its handler is not a registered ICommandHandler.
    // Skipped: Ping (Features/Health) — no command dispatch; a bare health probe.
}
