

namespace Jewel.JPMS.Pages;

public partial class DocumentControl
{
    private string? DestinationHref(DocumentControlItem item) => item.FiledAs switch
    {
        DocumentFiledAs.Drawing when item.FiledRecordId is not null =>
            ProjectFor(item) is { } drawingProject ? $"/projects/{drawingProject}/documents/{item.FiledRecordId}" : null,
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
        (view == tab
            ? "tab tab-active"
            : "tab");

    private string DestinationTabClass(FileDestination tab) =>
        (destination == tab
            ? "chip chip-active"
            : "chip");

    public void Dispose()
    {
        DrawingStore.OnChange -= StateHasChanged;
        ValuationReport.OnChange -= StateHasChanged;
        ProjectList.OnChanged -= StateHasChanged;
        Subcontractors.OnChanged -= StateHasChanged;
    }
}
