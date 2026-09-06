namespace Jewel.JPMS.Models;

/// <summary>
/// How a lead came to exist. <see cref="Strategy"/> means one of our own sales strategies found
/// it (the lead then carries the StrategyId); the rest are the ways leads arrive without our
/// going looking. Values persist as ints — append, never reorder. (The May 2026 list — Website,
/// Instagram, LinkedIn… — was a channel list, not a source list; retired 2026-09-06 and remapped
/// by the AddSalesStrategies migration.)
/// </summary>
public enum LeadSource
{
    Strategy = 0,
    Inbound = 1,
    Referral = 2,
    Architect = 3,
    RepeatClient = 4,
    Manual = 5
}

public static class LeadSourceExtensions
{
    public static string DisplayName(this LeadSource source) => source switch
    {
        LeadSource.Strategy     => "Sales strategy",
        LeadSource.Inbound      => "Inbound enquiry",
        LeadSource.Referral     => "Referral",
        LeadSource.Architect    => "Architect introduction",
        LeadSource.RepeatClient => "Repeat client",
        LeadSource.Manual       => "Added by hand",
        _ => source.ToString()
    };
}

/// <summary>
/// Who the prospect is — the kind of person we would be convincing. A homeowner is sold an
/// upgrade or a new home as an investment; an architect is sold Jewel as the builder that makes
/// their projects run; a developer is sold delivery. The strategy's audience picks the default.
/// Values persist as ints — append, never reorder.
/// </summary>
public enum LeadProspectKind
{
    Homeowner = 0,
    Architect = 1,
    Developer = 2,
    Landowner = 3,
    Business = 4,
    Other = 5
}

public static class LeadProspectKindExtensions
{
    public static string DisplayName(this LeadProspectKind kind) => kind switch
    {
        LeadProspectKind.Homeowner => "Homeowner",
        LeadProspectKind.Architect => "Architect",
        LeadProspectKind.Developer => "Developer",
        LeadProspectKind.Landowner => "Landowner",
        LeadProspectKind.Business  => "Business",
        LeadProspectKind.Other     => "Other",
        _ => kind.ToString()
    };
}
