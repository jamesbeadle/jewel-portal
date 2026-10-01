using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    /// <summary>The week review and the week planner from the connector (2026-10-01): a director
    /// signs a submitted week off or sends it back, and plans who is in, through the by-name
    /// wrappers over the portal's own handlers. list_submitted_weeks and view_week_plan are the
    /// reads that pair with these.</summary>
    private static IEnumerable<AiAction> WeekReviewActions() =>
        new[] { SignOffSubmittedWeekAction(), SendBackSubmittedWeekAction(), PlanWorkerDaysAction() };

    private static AiAction SignOffSubmittedWeekAction() =>
        new(
            Name: "sign_off_submitted_week",
            Area: "Labour",
            Description: "Signs off a week an operative submitted from My day, in one step — the "
                + "Labour overview's Weeks for review, by name: every waiting day in the week is "
                + "approved at the worker's rate under the same budget rules as the Labour tab, a "
                + "day the worker recorded off becomes a recorded absence, the week's sign-off "
                + "marker is written for the month-end, and the week locks with who signed and "
                + "when. A day the approval door refuses — over budget, uncoded — refuses the "
                + "whole sign-off naming the day; a day already sent back is answered by "
                + "send_back_submitted_week, never signed over.",
            CommandType: typeof(SignOffSubmittedWeekByName),
            ResultType: typeof(WorkerWeekSubmission),
            AuthorisationType: typeof(SignOffSubmittedWeekByNameAuthorisation),
            ValidationType: typeof(SignOffSubmittedWeekByNameValidation),
            VisibleTo: LabourRoleSets.ReviewWorkerWeeks,
            EmailStamps: new[] { "SignedOffByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "workerName as the user says it; weekStart is any date in the week. Show the "
                + "user the week first (list_submitted_weeks) and get their yes — signing off "
                + "posts real cost and arms the Xero coding run for that week. Only the MD, FD "
                + "and Admin may answer a submitted week.");

    private static AiAction SendBackSubmittedWeekAction() =>
        new(
            Name: "send_back_submitted_week",
            Area: "Labour",
            Description: "Sends a submitted week back to the operative with a note — the week "
                + "reopens on My day for them to amend and submit again; no timesheet changes.",
            CommandType: typeof(SendBackSubmittedWeekByName),
            ResultType: typeof(WorkerWeekSubmission),
            AuthorisationType: typeof(SendBackSubmittedWeekByNameAuthorisation),
            ValidationType: typeof(SendBackSubmittedWeekByNameValidation),
            VisibleTo: LabourRoleSets.ReviewWorkerWeeks,
            EmailStamps: new[] { "ReviewedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; weekStart is any date in the week; note is "
                + "what the operative reads, so say which day and what needs changing.");

    private static AiAction PlanWorkerDaysAction() =>
        new(
            Name: "plan_worker_days",
            Area: "Labour",
            Description: "The week planner's tap, by name: marks a worker's days not in (time off "
                + "they asked for, recorded as the office's absence — the overview, the chase list "
                + "and the sign-off gate already read it) or back in (the absence removed). A day "
                + "already logged cannot be planned off.",
            CommandType: typeof(PlanWorkerDaysByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(PlanWorkerDaysByNameAuthorisation),
            ValidationType: typeof(PlanWorkerDaysByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: new[] { "PlannedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; dates are the days, YYYY-MM-DD, any number; "
                + "isIn false marks them not in, true marks them in. view_week_plan shows the "
                + "week before and after.");
}
