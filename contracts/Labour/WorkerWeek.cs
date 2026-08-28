using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The accountant's weekly entry path (Labour overview → "Enter a week"): one worker's whole week
// of site days in a single act, transcribed from how the crews actually report — "Mon – Chislehurst,
// Tue – Chislehurst …" on WhatsApp. Every day lands as an ordinary Submitted timesheet on its own
// project — same validation, same approval queue, same budget hard-block at approval as any other
// entry. Days that already carry a timesheet or a recorded absence are SKIPPED, never overwritten:
// corrections stay on the project's Labour tab, where adjust/reject already live.

/// <summary>One day of the week being entered: which site (project), how long, and optionally the
/// cost code the hours are coded to. CostCode is OPTIONAL by design (decision 2026-08-21): the
/// transcriber records WHERE people were; the approver codes the day when approving, and
/// ApproveTimesheets refuses an uncoded day until it is coded — so leaving it blank never skips
/// the coding step, it enforces it.</summary>
public sealed record WorkerWeekDayEntry(
    DateTimeOffset Date,
    string ProjectId,
    decimal Hours,
    string? CostCode = null);

/// <summary>
/// Enters one worker's week in one command. WeekStart is the Monday; Days may be any subset of
/// that week (weekends included — crews do work Saturdays). Per-day outcomes report what was
/// created and what was skipped, so a partial landing is stated rather than guessed.
/// </summary>
public sealed record SubmitWorkerWeek(
    string WorkerId,
    DateTimeOffset WeekStart,
    IReadOnlyList<WorkerWeekDayEntry> Days) : ICommand<WorkerWeekResult>;

/// <summary>
/// The connector's shape of <see cref="SubmitWorkerWeek"/>: the same week entry keyed by the
/// worker's NAME as people actually say it ("Adam Turk"), resolved server-side against the worker
/// register. An AI caller has no worker-id picker, and the register's names — not emails, which
/// are only the optional link for a worker's own portal sign-in — are how timesheets identify
/// people. Delegates to the same handler as the Enter-a-week form, so the two entries cannot
/// drift: same skip rules, same Submitted status, same approval queue.
/// </summary>
public sealed record SubmitWorkerWeekByName(
    string WorkerName,
    DateTimeOffset WeekStart,
    IReadOnlyList<WorkerWeekDayEntry> Days) : ICommand<WorkerWeekResult>;

/// <summary>What happened to one submitted day. Created = a Submitted timesheet now exists;
/// otherwise Detail says why the day was skipped ("already recorded — Guildford, 8h").</summary>
public sealed record WorkerWeekDayOutcome(
    DateTimeOffset Date,
    bool Created,
    string Detail);

public sealed record WorkerWeekResult(
    string WorkerId,
    string WorkerName,
    IReadOnlyList<WorkerWeekDayOutcome> Outcomes);
