using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The connector's month-end leg (2026-08-31, the accountant's ask — "the whole month closes from
// one instruction"): sign off worker-weeks and run the Xero coding from an AI session the way the
// Labour overview does it — same handlers, same signable rule, same skip-and-report gates — keyed
// by worker NAME because an AI caller never holds the register's opaque worker ids. The chain the
// connector can now walk: view_labour_week → code/approve → sign_off_labour_week →
// run_xero_coding → (human approves the bill in Xero) → set_xero_line_timesheet_cover /
// add_labour_settlement_variance.

/// <summary>
/// Places the weekly sign-off marker on a worker's week — the Labour overview's Sign off, by
/// name. The server re-checks the signable rule at the moment of signing (every elapsed day of
/// the month's part of the week approved, rejected or recorded as absence) and refuses with the
/// reason when it fails. Sign-off freezes the week for settlement: fully signed-off worker-months
/// are what run_xero_coding will write to Xero. SignedOffByEmail is stamped server-side from the
/// connector caller.
/// </summary>
public sealed record SignOffWorkerWeekByName(
    string WorkerName,
    DateTimeOffset WeekStart,
    string SignedOffByEmail = "",
    // The month whose part of the week to sign (any date in it) — only matters for a week that
    // straddles a month end, which signs off in two parts (2026-09-02). Left out, the month of
    // WeekStart as given: pass 31 Aug for August's part of that week, 1 Sep for September's.
    DateTimeOffset? MonthStart = null) : ICommand<LabourWeekSignOff>;

/// <summary>
/// Removes the weekly sign-off marker from a worker's week — the undo of sign-off. Touches no
/// timesheet (sign-off is a marker over approval, never a second state machine); removing it
/// simply takes the week back out of settlement scope until it is signed off again.
/// </summary>
public sealed record RemoveWorkerWeekSignOffByName(
    string WorkerName,
    DateTimeOffset WeekStart,
    DateTimeOffset? MonthStart = null) : ICommand<Acknowledgement>;

/// <summary>
/// The §6a automation, run for one month from the connector — the Labour overview's "Code month
/// into Xero", by name: for each fully signed-off worker-month, find the worker's existing bill
/// for the month (draft or authorised — the cover route is the sole trader's normal path) and
/// recode its lines to the settlement schedule (Sites and Cost Code tracking per the
/// effective-dated mappings), keeping the bill's total, VAT treatment, status and cover; or stage
/// a draft bill only where no bill exists. A bill that cannot be recoded (paid, part-paid,
/// credited, voided) skips with its status named — a second bill is never staged beside one.
/// Mapping gaps, unsigned weeks and already-coded months skip-and-report per worker — the run
/// never guesses and never writes from unsigned data. WorkerNames narrows the run; null runs
/// every worker with activity in the month. RunByEmail is stamped server-side from the connector
/// caller. The report (XeroCodingRunReport, Models) is one row per worker with what happened —
/// BillRecoded / DraftStaged / Skipped / Failed — and the detail in the run's own words.
/// </summary>
public sealed record RunXeroCodingByName(
    int Year,
    int Month,
    IReadOnlyList<string>? WorkerNames = null,
    string RunByEmail = "") : ICommand<XeroCodingRunReport>;

/// <summary>
/// The dry run of <see cref="RunXeroCodingByName"/> (2026-09-03, item E of the accountant's
/// spec): the same gates, the same bill search, the same skip reasons — but it reports per
/// worker what the run WOULD do (WouldRecodeBill / WouldStageDraft / Skipped) and writes nothing,
/// to Xero or to the run history. Run it, show the list, get the yes, then run_xero_coding.
/// </summary>
public sealed record PreviewXeroCodingByName(
    int Year,
    int Month,
    IReadOnlyList<string>? WorkerNames = null) : ICommand<XeroCodingRunReport>;

/// <summary>
/// Resets a worker-month's coding outcome (2026-09-03, item D): the run-once gate reads the
/// LATEST recorded outcome, so a worker-month whose staged bill was later deleted by hand sits
/// behind DraftStaged for ever. The reset records a Reset outcome (who, why, what it was) —
/// history is appended, never erased — and the next run takes the month again. Touches nothing
/// in Xero. ResetByEmail is stamped server-side from the connector caller.
/// </summary>
public sealed record ResetXeroCodingOutcomeByName(
    string WorkerName,
    int Year,
    int Month,
    string Reason,
    string ResetByEmail = "") : ICommand<Acknowledgement>;
