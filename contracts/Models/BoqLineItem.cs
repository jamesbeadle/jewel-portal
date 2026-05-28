namespace Jewel.JPMS.Models;

public sealed record BoqLineItem(
    string BoqLineItemId,
    string ProjectId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal RateValue,
    string CostCode,
    Discipline Discipline)
{
    public decimal LineTotal => Quantity * RateValue;
}
