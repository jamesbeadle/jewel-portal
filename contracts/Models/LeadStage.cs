namespace Jewel.JPMS.Models;

/// <summary>
/// Where a lead has got to (Sales → Leads, rebuilt 2026-09-06). One short ladder — New →
/// Contacted → Engaged → Site visit → Proposal — ending Won or Lost, with Nurture for a lead that
/// is not live now but worth keeping warm. Deliberately coarse: a strategy is judged by how many
/// of its leads climb the ladder, so every strategy's leads must climb the SAME ladder. The May
/// 2026 13-stage pipeline (Qualified, Survey booked, Awaiting information, Feasibility…) was
/// retired with the rebuild; persisted ints were remapped by the AddSalesStrategies migration.
/// Values persist as ints — append, never reorder.
/// </summary>
public enum LeadStage
{
    New = 0,
    Contacted = 1,
    Engaged = 2,
    SiteVisit = 3,
    Proposal = 4,
    Won = 5,
    Lost = 6,
    Nurture = 7
}

public static class LeadStageExtensions
{
    /// <summary>The ladder in the order a lead climbs it — the open stages, then the outcomes.
    /// Pickers and boards render in this order, never enum order.</summary>
    public static readonly IReadOnlyList<LeadStage> Ladder = new[]
    {
        LeadStage.New, LeadStage.Contacted, LeadStage.Engaged, LeadStage.SiteVisit, LeadStage.Proposal,
        LeadStage.Won, LeadStage.Lost, LeadStage.Nurture
    };

    public static string DisplayName(this LeadStage stage) => stage switch
    {
        LeadStage.New       => "New",
        LeadStage.Contacted => "Contacted",
        LeadStage.Engaged   => "Engaged",
        LeadStage.SiteVisit => "Site visit",
        LeadStage.Proposal  => "Proposal",
        LeadStage.Won       => "Won",
        LeadStage.Lost      => "Lost",
        LeadStage.Nurture   => "Nurture",
        _ => stage.ToString()
    };

    /// <summary>One line on what the stage means, for pickers and the board's column headers.</summary>
    public static string Meaning(this LeadStage stage) => stage switch
    {
        LeadStage.New       => "Found or received — nobody has spoken to them yet.",
        LeadStage.Contacted => "We have reached out; no reply or conversation yet.",
        LeadStage.Engaged   => "They are talking to us about a possible project.",
        LeadStage.SiteVisit => "A visit is booked or has happened.",
        LeadStage.Proposal  => "A proposal or budget has gone to them.",
        LeadStage.Won       => "They have chosen Jewel — a client and a project exist.",
        LeadStage.Lost      => "Not going ahead with us.",
        LeadStage.Nurture   => "Not now, but worth keeping in touch with.",
        _ => ""
    };


    /// <summary>Still being worked — not Won, Lost or parked in Nurture.</summary>
    public static bool IsOpen(this LeadStage stage) =>
        stage is not (LeadStage.Won or LeadStage.Lost or LeadStage.Nurture);

    /// <summary>A closed outcome: Won or Lost. Nurture is neither — it can be reopened.</summary>
    public static bool IsOutcome(this LeadStage stage) => stage is LeadStage.Won or LeadStage.Lost;
}
