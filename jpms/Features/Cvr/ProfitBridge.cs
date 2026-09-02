namespace Jewel.JPMS.Features.Cvr;

/// <summary>One bar of the budget → forecast bridge. The waterfall geometry is precomputed by
/// the page — values mapped into a 12%–100% vertical band, the top 12% reserved for the amount
/// labels — so the chart only places rectangles.</summary>
public sealed record BridgeBar(
    string Label, string Sub, string Amount, string BarClass, string LabelClass,
    double Top, double Height, double? ConnectorY);

/// <summary>The bridge: its subtitle, where zero sits, and the four bars.</summary>
public sealed record BridgeModel(string Note, double ZeroY, IReadOnlyList<BridgeBar> Bars);
