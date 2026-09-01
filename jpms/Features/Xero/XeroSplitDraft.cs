namespace Jewel.JPMS.Features.Xero;

/// <summary>One row of a split being keyed: a project, a cost centre and its share of the net.</summary>
public sealed class XeroSplitDraft
{
    public string ProjectId { get; set; } = "";
    public string Code { get; set; } = "";
    public decimal? Amount { get; set; }
}
