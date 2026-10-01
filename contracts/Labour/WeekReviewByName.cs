using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>The connector's answer to a submitted week, keyed by worker NAME and any date in the
/// week because an AI caller never holds the submission's opaque id: signs the week off through the
/// same handler the Labour overview's Weeks for review uses. SignedOffByEmail is stamped server-side
/// from the connector caller.</summary>
public sealed record SignOffSubmittedWeekByName(
    string WorkerName,
    DateTimeOffset WeekStart,
    string SignedOffByEmail = "") : ICommand<WorkerWeekSubmission>;

/// <summary>Sends a submitted week back to the operative with the note they read on My day, by
/// worker name and any date in the week. ReviewedByEmail is stamped server-side.</summary>
public sealed record SendBackSubmittedWeekByName(
    string WorkerName,
    DateTimeOffset WeekStart,
    string Note,
    string ReviewedByEmail = "") : ICommand<WorkerWeekSubmission>;

/// <summary>The week planner's tap from the connector: a worker's days marked in or not in, by
/// name and date, through the same handler the planner page uses. PlannedByEmail is stamped
/// server-side.</summary>
public sealed record PlanWorkerDaysByName(
    string WorkerName,
    IReadOnlyList<DateTimeOffset> Dates,
    bool IsIn,
    string PlannedByEmail = "") : ICommand<Acknowledgement>;
