namespace Jewel.JPMS.Models;

/// <summary>
/// A lead (Sales → Leads, rebuilt 2026-09-06): a person we might convince to build with Jewel —
/// to upgrade their house or have a new one built — and the property or site the work would be
/// on. Every lead lands in the one register whatever found it: a sales strategy (then
/// <see cref="StrategyId"/> says which, and the strategy's funnel counts it), an inbound enquiry,
/// a referral, an architect, a past client, or someone typing it in. It climbs the
/// <see cref="LeadStage"/> ladder; Won creates the Client and the project shell
/// (<see cref="ClientId"/> / <see cref="ProjectId"/>), Lost keeps its reason.
/// </summary>
public sealed record Lead(
    string LeadId,
    // Sequential human reference ("LD-0001"), minted by the server.
    string Reference,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CompanyName,
    LeadProspectKind ProspectKind,
    // The property or site the work would be on, and its postcode on its own so leads group by
    // area (the postcode is what the house-price strategies target).
    string PropertyAddress,
    string Postcode,
    // One line on what the work might be — "rear extension and loft", "new build on the plot".
    string Summary,
    string Notes,
    LeadSource Source,
    // The strategy that found this lead (Source = Strategy), with its name carried for lists.
    string? StrategyId,
    string? StrategyName,
    LeadStage Stage,
    DateTimeOffset StageChangedAt,
    decimal? EstimatedValue,
    // Portal email of the staff member working the lead.
    string OwnerEmail,
    DateTimeOffset CapturedAt,
    // Set by Won: the client account and project shell the lead became.
    string? ClientId,
    string? ProjectId,
    // Set by Lost (and cleared on reopen): why not.
    string? LostReason);

/// <summary>What kind of touch an activity records. Values persist as ints — append only.</summary>
public enum LeadActivityKind
{
    Note = 0,
    Call = 1,
    Email = 2,
    Meeting = 3,
    SiteVisit = 4,
    Proposal = 5,
    StageChange = 6,
    Letter = 7
}

public static class LeadActivityKindExtensions
{
    public static string DisplayName(this LeadActivityKind kind) => kind switch
    {
        LeadActivityKind.Note        => "Note",
        LeadActivityKind.Call        => "Call",
        LeadActivityKind.Email       => "Email",
        LeadActivityKind.Meeting     => "Meeting",
        LeadActivityKind.SiteVisit   => "Site visit",
        LeadActivityKind.Proposal    => "Proposal",
        LeadActivityKind.StageChange => "Stage change",
        LeadActivityKind.Letter      => "Letter / brochure",
        _ => kind.ToString()
    };

    /// <summary>The kinds a person logs by hand; StageChange is written by the stage moves.</summary>
    public static readonly IReadOnlyList<LeadActivityKind> Loggable = new[]
    {
        LeadActivityKind.Note, LeadActivityKind.Call, LeadActivityKind.Email, LeadActivityKind.Meeting,
        LeadActivityKind.SiteVisit, LeadActivityKind.Proposal, LeadActivityKind.Letter
    };
}

/// <summary>One touch on a lead — a call made, a brochure posted, a note, a stage move — the
/// lead's timeline, newest first.</summary>
public sealed record LeadActivity(
    string LeadActivityId,
    string LeadId,
    LeadActivityKind Kind,
    string Summary,
    DateTimeOffset OccurredAt,
    string RecordedByEmail);

/// <summary>A lead with its timeline — the lead page's read.</summary>
public sealed record LeadDetail(Lead Lead, IReadOnlyList<LeadActivity> Activities);

/// <summary>What Won produced: the lead as it now stands, and the client and project it became.</summary>
public sealed record LeadWonOutcome(Lead Lead, string ClientId, string ProjectId);
