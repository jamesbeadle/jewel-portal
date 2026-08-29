using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>All timesheets for a project with labour detail (worker names, status, £ for
/// commercial roles). The Labour tab filters to a week client-side.</summary>
public sealed record ListTimesheetDetailsForProject(string ProjectId)
    : IQuery<IReadOnlyList<TimesheetDetail>>;

/// <summary>PM manual entry on a worker's behalf (missed sign-out path). Creates a
/// Submitted timesheet for the given worked date.</summary>
public sealed record AddWorkerTimesheet(string ProjectId, string WorkerId, DateTimeOffset WorkedOn,
    decimal Hours, string CostCode) : ICommand<TimesheetDetail>;

/// <summary>PM correction before approval: change hours and/or re-code to another cost code.</summary>
public sealed record AdjustTimesheet(string TimesheetId, decimal Hours, string CostCode)
    : ICommand<TimesheetDetail>;

/// <summary>
/// Batch approval. Per timesheet: resolves the worker's rate effective on the worked date,
/// snapshots rate and cost, and enforces the budget hard-block (workflow 07-D) — approval is
/// rejected for a cost code whose remaining budget the new cost would exceed. Partial success:
/// approvable timesheets approve, failures return with reasons.
///
/// AllowOverBudget (2026-08-29, the accountant's ask) is the deliberate exception to the
/// hard-block: the MD/FD/Admin only (endpoint-gated), with a mandatory reason, approves the
/// blocked rows anyway — the cost still posts, the overspend stays red on Financials, and each
/// overridden row is written to the audit trail with who, why, and the block it overrode. It
/// never bypasses the other refusals (uncoded, no worker, already decided).
/// </summary>
public sealed record ApproveTimesheets(string ProjectId, IReadOnlyList<string> TimesheetIds,
    bool AllowOverBudget = false, string OverBudgetReason = "")
    : ICommand<LabourApprovalResult>;

/// <summary>Rejection re-opens the day for the worker on the capture page; no deadline is
/// enforced and the PM can always correct and approve on the worker's behalf instead.</summary>
public sealed record RejectTimesheet(string TimesheetId, string Reason) : ICommand<TimesheetDetail>;
