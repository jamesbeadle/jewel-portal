
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
    // column (accountant 2026-08-26) — but the money columns must hold their widest figure on
    // ONE line first (accountant 2026-09-02: negatives printed as a bare "-" with the figure
    // spilling onto the next line). Sized for the host's DejaVu Sans, the widest face the
    // resolver may pick: a six-figure negative on a line and a seven-figure bold total both
    // fit MoneyWidthCentimetres at the money font sizes ValuationReportSnapshotRenderer uses.
    public double CodeWidthCentimetres => 1.1;
    public double ClientReferenceWidthCentimetres => 1.1;
    public double DescriptionWidthCentimetres => HasClientReference ? 4.15 : 5.25;
    public double QuantityWidthCentimetres => 0.8;
    public double RateWidthCentimetres => 1.2;
    public double AmountWidthCentimetres => MoneyWidthCentimetres;
    public double PercentWidthCentimetres => 0.85;
    public double PreviousWidthCentimetres => MoneyWidthCentimetres;
    public double PeriodWidthCentimetres => MoneyWidthCentimetres;
    public double ClaimedWidthCentimetres => MoneyWidthCentimetres;

    private const double MoneyWidthCentimetres = 2.15;
}
