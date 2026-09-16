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
/// left Submitted as history. HouseModelJson is the 3D model the lead page builds (2026-09-16),
/// with the sheets and revision it was read from — null until the assistant drafts one.
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
    DateTimeOffset CreatedAt,
    // The client-facing narrative (2026-09-15, the tender's shape): what we understand the
    // project to be and how we would approach it; how long it takes; what the figure leaves out.
    string ExecutiveSummary = "",
    string BuildTime = "",
    string Exclusions = "",
    // The priced breakdown — sections of lines, in print order. Empty until SetEstimateBreakdown;
    // when it has lines, Total is their sum.
    IReadOnlyList<EstimateLine>? Lines = null,
    string? HouseModelJson = null,
    string HouseModelSource = "",
    DateTimeOffset? HouseModelSetAt = null)
{
    public bool HasHouseModel => !string.IsNullOrWhiteSpace(HouseModelJson);

    public IReadOnlyList<EstimateLine> BreakdownLines => Lines ?? Array.Empty<EstimateLine>();

    /// <summary>The lines grouped into their sections in print order — what the sheet and the
    /// editor both walk.</summary>
    public IReadOnlyList<EstimateBreakdownSection> Sections =>
        BreakdownLines
            .GroupBy(line => (line.SectionOrder, line.Section, line.SectionProvisional))
            .OrderBy(group => group.Key.SectionOrder)
            .Select(group => new EstimateBreakdownSection(
                group.Key.Section,
                group.Key.SectionProvisional,
                group.OrderBy(line => line.SortOrder)
                    .Select(line => new EstimateBreakdownLine(line.CostCode, line.Description, line.Quantity, line.Unit, line.UnitPrice))
                    .ToList()))
            .ToList();
}

/// <summary>
/// One priced line of an estimate's breakdown (2026-09-15): the tender's row — a cost code from
/// the cost-centre master (the ID column; blank allowed), what, how many, of what, at what rate,
/// and the line total (quantity × unit price, computed server-side). Section is the heading the
/// line prints under ("Preliminaries &amp; preambles", "Structural steelwork"), carried on every
/// line with its order and its provisional flag; SortOrder is the line's place in it.
/// </summary>
public sealed record EstimateLine(
    string LineId,
    string EstimateId,
    string Section,
    int SectionOrder,
    bool SectionProvisional,
    string CostCode,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal Total,
    int SortOrder);

/// <summary>A section of the breakdown as written — the shape SetEstimateBreakdown takes and the
/// editor edits. Provisional marks an allowance (the tender prints it in red).</summary>
public sealed record EstimateBreakdownSection(string Name, bool Provisional, IReadOnlyList<EstimateBreakdownLine> Lines)
{
    public decimal Total => Lines.Sum(line => line.Total);
}

/// <summary>A line as written: quantity × unit price is its total.</summary>
public sealed record EstimateBreakdownLine(string CostCode, string Description, decimal Quantity, string Unit, decimal UnitPrice)
{
    public decimal Total => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
}
