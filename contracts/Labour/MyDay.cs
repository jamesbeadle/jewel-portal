using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>Today's state for the signed-in worker: their assigned projects with sign-in/out state,
/// the allocation cost codes and the day's note once it is logged, plus any rejected timesheets
/// awaiting resubmission. The caller is a normal portal user resolved to their Worker record by
/// their signed-in email; no rates or £ in any of these shapes.</summary>
public sealed record GetMyLabourDay : IQuery<MyLabourDay>;

/// <summary>Sign in on arrival — creates today's site-register row, the health-and-safety record.
/// <paramref name="SignedInAt"/> is the time now unless the worker adjusts it because they forgot
/// at the gate; it must be a time today that has passed. Idempotent per day.</summary>
public sealed record MySiteSignIn(string ProjectId, DateTimeOffset? SignedInAt = null) : ICommand<Acknowledgement>;

/// <summary>
/// The day, logged once at sign-out: the hours per cost code (one Submitted timesheet each), the
/// words of what was done — required, because they are the day's note on the project's progress
/// feed in the worker's own name, which the Contractor's Report reads — and the sign-out time,
/// now unless adjusted. Photographs follow onto the note. One sign-out per project per day.
/// Beside the note, and never inside its words: an instruction given on site is raised as a
/// Site Instruction and a defect as a defect, both in the worker's name for the office to triage.
/// </summary>
public sealed record MySiteSignOut(
    string ProjectId,
    IReadOnlyList<SiteSignOutEntry> Entries,
    string Description = "",
    DateTimeOffset? SignedOutAt = null,
    SiteLogInstruction? Instruction = null,
    string Defect = "") : ICommand<MySiteDayLogged>;

/// <summary>A day off, recorded as a day: the words say why (rain, holiday, no works on site) and
/// nothing else is written — no attendance, no hours. A recorded off day is never a missing day.</summary>
public sealed record MySiteDayOff(string ProjectId, string Description) : ICommand<MySiteDayLogged>;

/// <summary>Resubmits one of the caller's own rejected timesheets (back to Submitted).</summary>
public sealed record MyResubmitTimesheet(string TimesheetId, decimal Hours, string CostCode)
    : ICommand<Acknowledgement>;
