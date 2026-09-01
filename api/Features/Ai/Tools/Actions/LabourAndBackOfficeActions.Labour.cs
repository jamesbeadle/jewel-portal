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

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> LabourActions() => new AiAction[]
    {
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
                + "rather than guessing (link_worker_to_company can do it by name later). "
                + "isSoleTrader marks a worker who bills under their own name (their own "
                + "settlement counterparty); engagedFrom/engagedTo bound what the chase list "
                + "expects. A worker added by mistake can be deactivated on the Workers page."),

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

        // ---- Worker directory links & the chase list (2026-08-31, month-end doc items A–H) ----

        new AiAction(
            Name: "link_worker_to_company",
            Area: "Labour",
            Description: "Links a worker to a directory company — the settlement identity the "
                + "whole labour/Xero machinery keys on: covers, the settlement schedule and the "
                + "coding run all reconcile through it. Both names are matched server-side "
                + "(worker against the register, company against the non-prospect directory) and "
                + "an ambiguous name refuses with the candidates. Clears any sole-trader flag — "
                + "a company link always wins. Audited.",
            CommandType: typeof(LinkWorkerToCompanyByName),
            ResultType: typeof(Worker),
            AuthorisationType: typeof(LinkWorkerToCompanyByNameAuthorisation),
            ValidationType: typeof(LinkWorkerToCompanyByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName and companyName as the user says them. If the company exists only "
                + "in Xero, import it first (import_xero_supplier) — the import now auto-links "
                + "workers whose names match. For a worker who bills under their OWN name, "
                + "set_worker_sole_trader is the right fix, never an invented directory company."),

        new AiAction(
            Name: "set_worker_sole_trader",
            Area: "Labour",
            Description: "Flags (or with isSoleTrader: false unflags) a worker as a sole trader "
                + "who bills Dext/Xero under their own name — the worker then becomes their own "
                + "settlement counterparty: their bills can be marked as settlement, the "
                + "settlement schedule reconciles them, and the coding run stages draft bills "
                + "under their name. Refused while a company link exists (the link always wins — "
                + "clear it first). Audited.",
            CommandType: typeof(SetWorkerSoleTraderByName),
            ResultType: typeof(Worker),
            AuthorisationType: typeof(SetWorkerSoleTraderByNameAuthorisation),
            ValidationType: typeof(SetWorkerSoleTraderByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it. This is the designed answer for sole traders "
                + "(Midgley, Downey, Everitt, Jancauskas and the like) — never create a directory "
                + "company that does not exist just to unblock settlement."),

        new AiAction(
            Name: "reconcile_worker_directory_links",
            Area: "Labour",
            Description: "Sweeps every active worker with no settlement identity against the "
                + "company directory by name (the same matching the allocation page's labour "
                + "recognition uses) — the backfill for contacts imported before the import "
                + "auto-linked. apply: false reports what WOULD link, plus the ambiguous and "
                + "unmatched workers, without writing anything; apply: true writes the "
                + "unambiguous links (audited per worker) and still reports the remainder for a "
                + "human decision.",
            CommandType: typeof(ReconcileWorkerDirectoryLinks),
            ResultType: typeof(WorkerDirectoryLinkReport),
            AuthorisationType: typeof(ReconcileWorkerDirectoryLinksAuthorisation),
            ValidationType: typeof(ReconcileWorkerDirectoryLinksValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: new[] { "LinkedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Run apply: false FIRST and show the user the would-link list — the confirm "
                + "turn then has real names in it, not a promise. Unmatched workers are usually "
                + "sole traders (set_worker_sole_trader) or companies still to import "
                + "(import_xero_supplier); ambiguous ones need link_worker_to_company with the "
                + "exact company name."),

        new AiAction(
            Name: "dismiss_labour_chase_day",
            Area: "Labour",
            Description: "Dismisses one worker's chase-list day with a mandatory reason — the "
                + "day was reviewed and needs no timesheet and no absence. The day leaves the "
                + "chase list AND the unconfirmed-cost accrual, so the confidence figures follow "
                + "the decision; the dismissal is written to the audit trail, and a timesheet or "
                + "absence recorded later supersedes it naturally.",
            CommandType: typeof(DismissLabourChaseDayByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DismissLabourChaseDayByNameAuthorisation),
            ValidationType: typeof(DismissLabourChaseDayByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: new[] { "DismissedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; date is the single chase day. reason is "
                + "mandatory and shows on the audit trail — write the actual reason (\"not on "
                + "site that week\", \"engagement ended mid-month\"), not \"clearing the list\". "
                + "A worker wrongly chased EVERY day usually needs the real fix instead: "
                + "contracted days, a project assignment, or engagement dates on the worker. "
                + "restore_labour_chase_day is the undo."),

        new AiAction(
            Name: "restore_labour_chase_day",
            Area: "Labour",
            Description: "Removes a chase-day dismissal, putting the day back on the chase list "
                + "and back into the unconfirmed-cost accrual — the undo of "
                + "dismiss_labour_chase_day.",
            CommandType: typeof(RestoreLabourChaseDayByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RestoreLabourChaseDayByNameAuthorisation),
            ValidationType: typeof(RestoreLabourChaseDayByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; date is the dismissed day."),

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

    };
}
