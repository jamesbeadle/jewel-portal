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
/// The §6a automation, run for one month from the connector — the Labour overview's "Run Xero
/// coding", by name: for each fully signed-off worker-month, recode the covered Dext draft bill
/// to the settlement schedule (Sites and Cost Code tracking per the effective-dated mappings) or
/// stage a draft bill where none has arrived. Everything lands DRAFT in Xero; approval there
/// stays human. Mapping gaps, unsigned weeks, open variances and already-coded months
/// skip-and-report per worker — the run never guesses and never writes from unsigned data.
/// WorkerNames narrows the run; null runs every worker with activity in the month.
/// RunByEmail is stamped server-side from the connector caller.
/// </summary>
public sealed record RunXeroCodingByName(
    int Year,
    int Month,
    IReadOnlyList<string>? WorkerNames = null,
    string RunByEmail = "") : ICommand<XeroCodingRunReport>;

/// <summary>The run's per-worker outcomes, reported the way approval outcomes are: one row per
/// worker with what happened (BillRecoded / DraftStaged / Skipped / Failed) and the detail in the
/// run's own words.</summary>
public sealed record XeroCodingRunReport(
    int Year,
    int Month,
    IReadOnlyList<XeroCodingRunResult> Outcomes);
