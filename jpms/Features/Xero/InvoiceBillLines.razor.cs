namespace Jewel.JPMS.Features.Xero;

public partial class InvoiceBillLines
{
    /// <summary>The line the window was opened from — its bill is what is fetched.</summary>
    [Parameter, EditorRequired] public XeroLedgerLine Line { get; set; } = default!;

    /// <summary>Names come from the masters the page holds; the card only displays them.</summary>
    [Parameter, EditorRequired] public Func<string?, string> ProjectName { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string?, string> CostCenterText { get; set; } = default!;

    private string? loadedInvoiceId;
    private IReadOnlyList<XeroLedgerLine>? lines;
    private bool fetchFailed;

    private string? Reference => XeroLedgerDisplay.ReferenceBeyondInvoiceNumber(Line);

    private bool ShowsLines => lines is { Count: > 1 };

    private IReadOnlyList<XeroLedgerLine> Lines => lines ?? Array.Empty<XeroLedgerLine>();

    protected override async Task OnParametersSetAsync()
    {
        if (Line.XeroInvoiceId == loadedInvoiceId) return;
        var fetching = loadedInvoiceId = Line.XeroInvoiceId;
        lines = null;
        fetchFailed = false;
        try
        {
            var fetched = await Queries.AskAsync(new ListXeroLedgerLinesForInvoice(fetching), CancellationToken.None);
            if (loadedInvoiceId != fetching) return; // Re-pointed at another bill mid-fetch.
            lines = fetched;
        }
        catch (Exception)
        {
            if (loadedInvoiceId != fetching) return;
            fetchFailed = true;
        }
    }

    private string StandingOf(XeroLedgerLine line) => line.AllocationStatus switch
    {
        XeroAllocationStatus.Allocated => AllocatedStandingOf(line),
        XeroAllocationStatus.Bucketed => $"Bucketed · {line.Bucket ?? "—"}",
        XeroAllocationStatus.Ignored => "Ignored",
        XeroAllocationStatus.Disputed => "Disputed",
        _ => line.ProjectId is null ? "Unallocated" : $"Unallocated · project set to {ProjectName(line.ProjectId)}"
    };

    private string AllocatedStandingOf(XeroLedgerLine line)
    {
        if (line.Splits is { Count: > 0 }) return $"Allocated · split {line.Splits.Count} ways";
        return $"Allocated · {ProjectName(line.ProjectId)} · {CostCenterText(line.CostCenterCode)}";
    }
}
