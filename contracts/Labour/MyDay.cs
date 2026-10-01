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
/// <paramref name="ProjectId"/> is the site signed in at; <paramref name="SiteProjectId"/>, when
/// given, is the site the work was actually on — another of the worker's own — and the whole day
/// (attendance, hours and note) is written there instead (Jeremy on Jack's phone, 30 Sep 2026).
/// </summary>
public sealed record MySiteSignOut(
    string ProjectId,
    IReadOnlyList<SiteSignOutEntry> Entries,
    string Description = "",
    DateTimeOffset? SignedOutAt = null,
    SiteLogInstruction? Instruction = null,
    string Defect = "",
    string SiteProjectId = "") : ICommand<MySiteDayLogged>;

/// <summary>A day off, recorded as a day: the words say why (rain, holiday, no works on site) and
/// nothing else is written — no attendance, no hours. A recorded off day is never a missing day.
/// <paramref name="Date"/> is today unless the worker is filling in a day they missed, and then a
/// working day already gone, within the late-filing window (Jeremy on Jack's phone, 30 Sep 2026).</summary>
public sealed record MySiteDayOff(string ProjectId, string Description, DateTimeOffset? Date = null) : ICommand<MySiteDayLogged>;

/// <summary>A day the worker was on site but never logged — no signal, forgot — filled in after
/// the date from My day's week (Jeremy on Jack's phone, 30 Sep 2026: Monday read "Nothing" and
/// could not be opened). Writes the day's Submitted timesheet and its note, both marked as filed
/// late for the office, and no attendance: there was no sign-in at the gate to record. The date
/// must be a working day already gone, within the late-filing window; a day already logged or
/// recorded off is amended instead. Photographs follow onto the note as they do at sign-out.</summary>
public sealed record MyLogMissedSiteDay(
    string ProjectId,
    DateTimeOffset Date,
    decimal Hours,
    string CostCode,
    string Description) : ICommand<MySiteDayLogged>;

/// <summary>Amends a day the worker has logged, until the office has approved it: the hours and
/// cost code of that day's timesheet, the words of its note and its sign-out time, in one save
/// (Jeremy on Jack's phone, 29 Sep 2026: "I can't amend or change something on this entry?").
/// An approved day is refused — that is the Project Manager's to change. The day's instruction
/// and defect were raised when it was logged and are not re-raised here. <paramref name="ProjectId"/>
/// is the site the day belongs on — another of the worker's sites when it was logged against the
/// wrong one (Jeremy, 30 Sep 2026), blank to leave it where it is; the timesheet, the note and the
/// attendance move together. A day amended after its date is marked as filed late for the office.</summary>
public sealed record MyAmendSiteDay(
    string TimesheetId,
    decimal Hours,
    string CostCode,
    string Description,
    DateTimeOffset? SignedOutAt = null,
    string ProjectId = "") : ICommand<Acknowledgement>;

/// <summary>Resubmits one of the caller's own rejected timesheets (back to Submitted).</summary>
public sealed record MyResubmitTimesheet(string TimesheetId, decimal Hours, string CostCode)
    : ICommand<Acknowledgement>;
