namespace Jewel.JPMS.Features.Sales;

/// <summary>What the estimate document says, as typed: the three texts that print after the project page.</summary>
public sealed record EstimateNarrative(string ExecutiveSummary, string BuildTime, string Exclusions);
