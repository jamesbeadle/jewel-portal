using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;


namespace Jewel.JPMS.Pages;

public partial class DocumentControl
{
    // ---- Display helpers ----

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

    private static string DisplaySender(DocumentControlItem item) =>
        !string.IsNullOrWhiteSpace(item.FromName) ? item.FromName
        : !string.IsNullOrWhiteSpace(item.FromEmail) ? item.FromEmail
        : "Unknown sender";

    private static bool IsPdf(DocumentControlItem item) =>
        (item.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || item.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    private static bool IsImage(DocumentControlItem item) =>
        (item.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static bool IsZip(DocumentControlItem item) =>
        item.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        || (item.ContentType ?? "").Contains("zip", StringComparison.OrdinalIgnoreCase);

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


    private static string Date(DateTimeOffset value) => value.LocalDateTime.ToString("d MMM yyyy, HH:mm");

    private static string ListDate(DateTimeOffset value)
    {
        var local = value.LocalDateTime;
        var today = DateTime.Today;
        if (local.Date == today) return local.ToString("HH:mm");
        if (local.Date == today.AddDays(-1)) return $"Yesterday {local:HH:mm}";
        if (local.Date > today.AddDays(-6)) return local.ToString("ddd HH:mm");
        return local.ToString("d MMM yyyy");
    }

    public void Dispose()
    {
        DrawingStore.OnChange -= StateHasChanged;
        ValuationReport.OnChange -= StateHasChanged;
        ProjectList.OnChanged -= StateHasChanged;
        Subcontractors.OnChanged -= StateHasChanged;
    }
}
