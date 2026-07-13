using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The worker's own timesheet surface ("My day"). Fully authenticated — the caller is a normal
// portal user (SiteOperative role, invited and password-set like anyone else) and is resolved
// to their Worker record by their signed-in email. No rates or £ in any of these shapes.

/// <summary>Today's state for the signed-in worker: their assigned projects with sign-in/out
/// state and allocation cost codes, plus any rejected timesheets awaiting resubmission.</summary>
public sealed record GetMyLabourDay : IQuery<MyLabourDay>;

/// <summary>Sign in on arrival — creates today's site-register row. Idempotent per day.</summary>
public sealed record MySiteSignIn(string ProjectId) : ICommand<Acknowledgement>;

/// <summary>
/// End-of-day allocation + sign-out. One Submitted timesheet per entry, attendance closed.
/// Server enforces: signed in today, not already signed out, ≥1 entry, half-hour steps,
/// cost codes from the project's list. One sign-out per project per day.
/// </summary>
public sealed record MySiteSignOut(string ProjectId, IReadOnlyList<SiteSignOutEntry> Entries)
    : ICommand<Acknowledgement>;

/// <summary>Resubmits one of the caller's own rejected timesheets (back to Submitted).</summary>
public sealed record MyResubmitTimesheet(string TimesheetId, decimal Hours, string CostCode)
    : ICommand<Acknowledgement>;
