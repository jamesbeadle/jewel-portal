namespace Jewel.JPMS.Models;

/// <summary>
/// A sales strategy (Sales → Strategies, 2026-09-06): a methodology for FINDING leads, written
/// down with its justification. Jewel gets clients many ways — homeowners in the postcodes where
/// house prices are about to move, sold building now as an investment; architects, sold the
/// portal's project management as the reason their jobs run smoothly; referrers, developers —
/// and the site must not be built for one. So a strategy is data: who it targets
/// (<see cref="Audience"/>), where (<see cref="TargetArea"/>), the hypothesis and the evidence
/// behind it, the channel used to reach them, and an approach plan (drafted by Claude from those
/// inputs, then edited by hand). Every lead a strategy finds carries its StrategyId, so the
/// strategy's funnel — leads found → engaged → won — is what says whether the methodology works.
/// Methodologies get better over time; the record is meant to be revised.
/// </summary>
public sealed record SalesStrategy(
    string StrategyId,
    string Name,
    SalesAudience Audience,
    // Where it applies: postcodes, towns, "within 20 miles of the office" — free text, one line.
    string TargetArea,
    // Why these people, why now — the argument in words.
    string Hypothesis,
    // What backs it: the data sources and findings (house-price trends, planning applications,
    // infrastructure news, Companies House…). Free text now; Phase 2 attaches research runs.
    string Evidence,
    SalesChannel Channel,
    // The pitch in one or two lines — what we would say to them.
    string Proposition,
    // The approach plan, markdown. Drafted by generate_strategy_plan / the page's Generate
    // button from the fields above; editable; blank until generated or written.
    string ApproachPlan,
    DateTimeOffset? PlanGeneratedAt,
    SalesStrategyStatus Status,
    string OwnerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Who the strategy targets. Values persist as ints — append only.</summary>
public enum SalesAudience
{
    Homeowners = 0,
    Architects = 1,
    Developers = 2,
    Landowners = 3,
    Referrers = 4,
    PastClients = 5,
    Other = 6
}

public static class SalesAudienceExtensions
{
    public static string DisplayName(this SalesAudience audience) => audience switch
    {
        SalesAudience.Homeowners  => "Homeowners",
        SalesAudience.Architects  => "Architects",
        SalesAudience.Developers  => "Developers",
        SalesAudience.Landowners  => "Landowners",
        SalesAudience.Referrers   => "Referrers (agents, planners, trades)",
        SalesAudience.PastClients => "Past clients",
        SalesAudience.Other       => "Other",
        _ => audience.ToString()
    };

    /// <summary>The prospect kind a lead found by this audience's strategy defaults to.</summary>
    public static LeadProspectKind DefaultProspectKind(this SalesAudience audience) => audience switch
    {
        SalesAudience.Homeowners  => LeadProspectKind.Homeowner,
        SalesAudience.Architects  => LeadProspectKind.Architect,
        SalesAudience.Developers  => LeadProspectKind.Developer,
        SalesAudience.Landowners  => LeadProspectKind.Landowner,
        SalesAudience.PastClients => LeadProspectKind.Homeowner,
        SalesAudience.Referrers   => LeadProspectKind.Business,
        _ => LeadProspectKind.Other
    };
}

/// <summary>How the strategy reaches people. Values persist as ints — append only.</summary>
public enum SalesChannel
{
    DirectMail = 0,
    Email = 1,
    Phone = 2,
    InPerson = 3,
    LinkedIn = 4,
    SocialMedia = 5,
    Events = 6,
    Partnerships = 7,
    Website = 8,
    Mixed = 9
}

public static class SalesChannelExtensions
{
    public static string DisplayName(this SalesChannel channel) => channel switch
    {
        SalesChannel.DirectMail   => "Post — letters and brochures",
        SalesChannel.Email        => "Email",
        SalesChannel.Phone        => "Phone",
        SalesChannel.InPerson     => "In person — door, office, site",
        SalesChannel.LinkedIn     => "LinkedIn",
        SalesChannel.SocialMedia  => "Social media",
        SalesChannel.Events       => "Events and talks",
        SalesChannel.Partnerships => "Partnerships and referrers",
        SalesChannel.Website      => "Website and content",
        SalesChannel.Mixed        => "Mixed",
        _ => channel.ToString()
    };
}

/// <summary>Draft while it is being written, Active while leads are being found under it, Paused
/// to stop without losing it, Retired when it has been judged. Values persist as ints.</summary>
public enum SalesStrategyStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Retired = 3
}

public static class SalesStrategyStatusExtensions
{
    public static string DisplayName(this SalesStrategyStatus status) => status switch
    {
        SalesStrategyStatus.Draft   => "Draft",
        SalesStrategyStatus.Active  => "Active",
        SalesStrategyStatus.Paused  => "Paused",
        SalesStrategyStatus.Retired => "Retired",
        _ => status.ToString()
    };

}

/// <summary>A strategy's funnel — how many leads it has found and how far they got. Counted
/// from the leads that carry its StrategyId; PipelineValue sums the open leads' estimates.</summary>
public sealed record SalesStrategyFunnel(
    int Leads,
    int Contacted,
    int Engaged,
    int Proposals,
    int Won,
    int Lost,
    int Nurture,
    decimal PipelineValue,
    decimal WonValue)
{
    public static readonly SalesStrategyFunnel Empty = new(0, 0, 0, 0, 0, 0, 0, 0m, 0m);

    /// <summary>Open leads — found but not yet Won, Lost or parked.</summary>
    public int Open => Leads - Won - Lost - Nurture;
}

/// <summary>A strategy with its funnel — the strategies list and the strategy page's header.</summary>
public sealed record SalesStrategyOverview(SalesStrategy Strategy, SalesStrategyFunnel Funnel);

/// <summary>A strategy, its funnel and the leads it has found — the strategy page's read.</summary>
public sealed record SalesStrategyDetail(SalesStrategy Strategy, SalesStrategyFunnel Funnel, IReadOnlyList<Lead> Leads);
