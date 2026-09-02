

namespace Jewel.JPMS.Pages;

public partial class DocumentControl
{
    private string? DestinationHref(DocumentControlItem item) => item.FiledAs switch
    {
        DocumentFiledAs.Drawing when item.FiledRecordId is not null =>
            ProjectFor(item) is { } drawingProject ? $"/projects/{drawingProject}/drawings/{item.FiledRecordId}" : null,
        DocumentFiledAs.PaymentCertificate => "/finance/payment-certificates",
        DocumentFiledAs.SubcontractorDocument => null, // the compliance row's home is the subcontractor record —
                                                       // but the record id is the document, so no deep link yet.
        _ => null
    };

    // Filed drawings know their project only through the label's project name — the hint is the
    // best cheap answer. (Good enough for the link; the drawing page itself is authoritative.)
    private string? ProjectFor(DocumentControlItem item) =>
        !string.IsNullOrWhiteSpace(item.ProjectIdHint) ? item.ProjectIdHint : null;

    private string? ProjectNameFor(string? projectId) =>
        string.IsNullOrWhiteSpace(projectId) ? null : ProjectList.Find(projectId)?.Name;

    private string ViewTabClass(DocView tab) =>
        "px-3 py-2 text-sm border-b-2 -mb-px transition "
        + (view == tab
            ? "border-accent text-content font-medium"
            : "border-transparent text-content-muted hover:text-content");

    private string DestinationTabClass(FileDestination tab) =>
        "rounded-md text-xs px-2.5 py-1.5 transition "
        + (destination == tab
            ? "bg-accent text-accent-ink font-medium"
            : "text-content-muted hover:bg-surface-raised");

    public void Dispose()
    {
        DrawingStore.OnChange -= StateHasChanged;
        ValuationReport.OnChange -= StateHasChanged;
        ProjectList.OnChanged -= StateHasChanged;
        Subcontractors.OnChanged -= StateHasChanged;
    }
}
