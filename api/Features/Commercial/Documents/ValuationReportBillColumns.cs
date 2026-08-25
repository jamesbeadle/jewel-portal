using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// The bill table's column positions on the valuation report PDF. The client-reference column
/// exists only when the project has mapped at least one cost centre to the client's
/// schedule-of-works references — every other client's statement keeps the layout it always
/// had — so every column after Code takes its index from here rather than a literal.
/// The widths sum to the A4 text width (17.8cm) in both layouts.
/// </summary>
internal sealed record ValuationReportBillColumns(bool HasClientReference)
{
    public static ValuationReportBillColumns For(IEnumerable<ValuationReportSnapshotLine> lines) =>
        new(lines.Any(line => !string.IsNullOrWhiteSpace(line.ClientReference)));

    public int Code => 0;
    public int ClientReference => 1;
    public int Description => HasClientReference ? 2 : 1;
    public int Quantity => Description + 1;
    public int Rate => Description + 2;
    public int Amount => Description + 3;
    public int Percent => Description + 4;
    public int Previous => Description + 5;
    public int Period => Description + 6;
    public int Claimed => Description + 7;
    public int Last => Claimed;

    public double CodeWidthCentimetres => 1.5;
    public double ClientReferenceWidthCentimetres => 1.3;
    public double DescriptionWidthCentimetres => HasClientReference ? 3.8 : 4.9;
    public double QuantityWidthCentimetres => HasClientReference ? 0.9 : 1.0;
    public double RateWidthCentimetres => 1.4;
    public double AmountWidthCentimetres => 1.9;
    public double PercentWidthCentimetres => HasClientReference ? 1.0 : 1.1;
    public double PreviousWidthCentimetres => 1.9;
    public double PeriodWidthCentimetres => 2.0;
    public double ClaimedWidthCentimetres => 2.1;
}
