using static Jewel.JPMS.Features.Sales.EstimateFigures;

namespace Jewel.JPMS.Features.Sales;

public sealed class EstimateSectionDraft
{
    public string Name { get; set; } = "";
    public bool Provisional { get; set; }
    public List<EstimateLineDraft> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(line => line.Total);
}

public sealed class EstimateLineDraft
{
    public string CostCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string QuantityText { get; set; } = "1";
    public string Unit { get; set; } = "";
    public string UnitPriceText { get; set; } = "";
    public decimal Quantity => ParseMoney(QuantityText) ?? 0m;
    public decimal UnitPrice => ParseMoney(UnitPriceText) ?? 0m;
    public decimal Total => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    public bool IsBlank => string.IsNullOrWhiteSpace(Description) && string.IsNullOrWhiteSpace(UnitPriceText);
}
