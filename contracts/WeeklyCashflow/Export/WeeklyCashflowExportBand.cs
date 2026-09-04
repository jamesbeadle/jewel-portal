namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>One band of the export — its grid label, its direction and its lines — computed once
/// and shared by every tab, so the tabs can never disagree with each other.</summary>
public sealed record WeeklyCashflowExportBand(
    WeeklyCashflowBand Band,
    string Label,
    IReadOnlyList<WeeklyCashflowExportLine> Lines)
{
    public bool IsCashIn => WeeklyCashflowMaths.IsCashIn(Band);

    public decimal AmountIn(int cellIndex) => Lines.Sum(line => line.AmountIn(cellIndex));

    public decimal Total => Lines.Sum(line => line.Total);
}
