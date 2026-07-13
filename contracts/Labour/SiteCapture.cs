using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// Site capture contracts — the anonymous, token-authenticated surface behind the project QR
// code. Token replaces the session; these must NEVER return rates or £ (see WorkerTimesheetView
// / SiteSignInSheet: hours-only shapes).

/// <summary>The sign-in sheet behind the QR code: assigned workers with their today-state, and
/// the project's active cost codes for the end-of-day allocation.</summary>
public sealed record GetSiteSignInSheet(string Token) : IQuery<SiteSignInSheet>;

/// <summary>Sign in on arrival. Creates the day's SiteAttendance (one per worker per day).</summary>
public sealed record SiteSignIn(string Token, string WorkerId) : ICommand<Acknowledgement>;

/// <summary>
/// End-of-day allocation + sign-out. Creates one Submitted timesheet per entry and closes the
/// attendance. Server enforces: signed in today, not already signed out, ≥1 entry, hours in
/// 0.5 steps ≥ 0.5, cost codes from the project's list. Totals over 12h are allowed (soft
/// warning client-side only).
/// </summary>
public sealed record SiteSignOut(string Token, string WorkerId, IReadOnlyList<SiteSignOutEntry> Entries)
    : ICommand<Acknowledgement>;

/// <summary>The worker's rejected timesheets for this project — re-opened days they can fix.</summary>
public sealed record ListWorkerRejectedTimesheets(string Token, string WorkerId)
    : IQuery<IReadOnlyList<WorkerTimesheetView>>;

/// <summary>Resubmits a rejected timesheet with corrected hours/cost code (back to Submitted).</summary>
public sealed record ResubmitTimesheet(string Token, string WorkerId, string TimesheetId,
    decimal Hours, string CostCode) : ICommand<Acknowledgement>;
