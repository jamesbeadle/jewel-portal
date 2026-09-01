
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

    // Description takes the room the numbers don't need — itemised lines wrap hard in a narrow
    // column (accountant 2026-08-26) and every other column holds figures of a known width.
    public double CodeWidthCentimetres => 1.4;
    public double ClientReferenceWidthCentimetres => 1.3;
    public double DescriptionWidthCentimetres => HasClientReference ? 4.9 : 6.2;
    public double QuantityWidthCentimetres => 0.8;
    public double RateWidthCentimetres => 1.2;
    public double AmountWidthCentimetres => 1.8;
    public double PercentWidthCentimetres => 0.9;
    public double PreviousWidthCentimetres => 1.8;
    public double PeriodWidthCentimetres => 1.8;
    public double ClaimedWidthCentimetres => 1.9;
}
