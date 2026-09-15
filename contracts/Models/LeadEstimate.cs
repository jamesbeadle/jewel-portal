namespace Jewel.JPMS.Models;

/// <summary>
/// Where an estimate has got to (2026-09-15). Received is the enquiry on the desk; Pricing is
/// Jewel working the figure up; Submitted is the price with the prospect; Won and Lost are how it
/// ended. Values persist as ints — append, never reorder.
/// </summary>
public enum EstimateStatus
{
    Received = 0,
    Pricing = 1,
    Submitted = 2,
    Won = 3,
    Lost = 4
}

public static class EstimateStatusExtensions
{
    /// <summary>The ladder in the order an estimate climbs it, then the outcomes.</summary>
    public static readonly IReadOnlyList<EstimateStatus> Ladder = new[]
    {
        EstimateStatus.Received, EstimateStatus.Pricing, EstimateStatus.Submitted,
        EstimateStatus.Won, EstimateStatus.Lost
    };

    public static string DisplayName(this EstimateStatus status) => status switch
    {
        EstimateStatus.Received  => "Received",
        EstimateStatus.Pricing   => "Pricing",
        EstimateStatus.Submitted => "Submitted",
        EstimateStatus.Won       => "Won",
        EstimateStatus.Lost      => "Lost",
        _ => status.ToString()
    };

    public static string Meaning(this EstimateStatus status) => status switch
    {
        EstimateStatus.Received  => "The enquiry is in — nobody has started pricing it.",
        EstimateStatus.Pricing   => "The figure is being worked up.",
        EstimateStatus.Submitted => "The price has gone to the prospect.",
        EstimateStatus.Won       => "They accepted the price.",
        EstimateStatus.Lost      => "They did not go ahead at this price.",
        _ => ""
    };

    /// <summary>Still being worked — not Won or Lost.</summary>
    public static bool IsOpen(this EstimateStatus status) =>
        status is not (EstimateStatus.Won or EstimateStatus.Lost);
}

/// <summary>
/// An estimate on a lead (2026-09-15, the estimating brief): Jewel's own pricing of one enquiry —
/// what the work is, who else is involved, when the price is due, what budget the prospect
/// mentioned, and the total once priced. It is the INTERNAL record the priced breakdown will hang
/// off once Nigel's estimating workbook is in the portal; the client-facing document built from
/// it is the <see cref="SalesProposal"/>. The EST-#### reference is minted server-side. A lead
/// may carry several — a re-price after a scope change is a new estimate, the old one Lost or
/// left Submitted as history.
/// </summary>
public sealed record LeadEstimate(
    string EstimateId,
    string LeadId,
    // Sequential human reference ("EST-0001"), minted by the server.
    string Reference,
    // What is to be priced — the type of work and its scope, in the estimator's words.
    string Scope,
    // The architect or consultant the enquiry came through, if any.
    string ArchitectName,
    // The date the prospect needs the price by; null when none was given.
    DateOnly? PriceDueOn,
    // Any budget the prospect mentioned, in pounds; null when none was mentioned.
    decimal? BudgetMentioned,
    // The priced total, in pounds; null until priced.
    decimal? Total,
    string Notes,
    EstimateStatus Status,
    DateTimeOffset StatusChangedAt,
    // When the price went to the prospect — stamped by the move to Submitted.
    DateTimeOffset? SubmittedAt,
    string CreatedByEmail,
    DateTimeOffset CreatedAt);
