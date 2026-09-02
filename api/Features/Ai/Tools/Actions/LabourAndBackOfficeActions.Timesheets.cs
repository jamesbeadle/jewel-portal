using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> TimesheetActions() => new AiAction[]
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
    };
}
