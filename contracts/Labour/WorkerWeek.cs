using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The accountant's weekly entry path (Labour overview → "Enter a week"): one worker's whole week
// of site days in a single act, transcribed from how the crews actually report — "Mon – Chislehurst,
// Tue – Chislehurst …" on WhatsApp. Every day lands as an ordinary Submitted timesheet on its own
// project — same validation, same approval queue, same budget hard-block at approval as any other
// entry. Days that already carry a timesheet or a recorded absence are SKIPPED, never overwritten:
// corrections stay on the project's Labour tab, where adjust/reject already live.

/// <summary>One day of the week being entered: which site (project), how long, and the cost code
/// the hours are coded to.</summary>
public sealed record WorkerWeekDayEntry(
    DateTimeOffset Date,
    string ProjectId,
    decimal Hours,
    string CostCode);

/// <summary>
/// Enters one worker's week in one command. WeekStart is the Monday; Days may be any subset of
/// that week (weekends included — crews do work Saturdays). Per-day outcomes report what was
/// created and what was skipped, so a partial landing is stated rather than guessed.
/// </summary>
public sealed record SubmitWorkerWeek(
    string WorkerId,
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
