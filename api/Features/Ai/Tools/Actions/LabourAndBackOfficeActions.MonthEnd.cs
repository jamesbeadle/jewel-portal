using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    // The whole month-end chain from the connector (2026-08-31, the accountant's ask): sign off
    // → run the coding → human approves the bill in Xero → mark the cover / post the variance.
    // view_settlement_month is the read that pairs with these.
    private static IEnumerable<AiAction> MonthEndActions() => new AiAction[]
    {
        new AiAction(
            Name: "sign_off_labour_week",
            Area: "Labour",
            Description: "Places the weekly sign-off marker on a worker's week — the Labour "
                + "overview's Sign off, by name. Sign-off freezes the week for settlement: only "
                + "fully signed-off worker-months are written to Xero by run_xero_coding. The "
                + "server re-checks the signable rule at the moment of signing — every elapsed "
                + "day must be approved, rejected or recorded as absence — and refuses with the "
                + "reason when it fails. A week that straddles a month end signs off PER MONTH: "
                + "its old-month days on their own (so the old month settles on the 1st) and its "
                + "new-month days later. Touches no timesheet; sign-off is a marker over "
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
                + "(normalised to that week's Monday). monthStart names the month whose part of "
                + "the week to sign — any date in that month — and matters only when the week "
                + "straddles a month end (view_worker_month marks those with monthPart); left "
                + "out, it is the month of the weekStart date you pass, so 31 Aug signs August's "
                + "days of that week and 1 Sep signs September's. Show the user the week first "
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
            Notes: "workerName as the user says it; weekStart is any date in the week; "
                + "monthStart picks the month's part of a week that straddles a month end (left "
                + "out: the month of weekStart as given). A worker-month the coding run has "
                + "ALREADY written stays written — removing a sign-off never un-codes a bill; "
                + "re-running after a schedule change is a deliberate human decision taken on "
                + "the settlement view."),

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
    };
}
