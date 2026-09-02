namespace Jewel.JPMS.Features.Requests;

/// <summary>How a request reads on its page — the reference it leads with, its dates, its
/// status colour — shared by the page and the header, facts and response components.</summary>
public static class RequestDisplay
{
    // The reference the page leads with: once promoted, the official instrument number (RFI-014) —
    // the number correspondents know it by — with the REQ container number as secondary context.
    // A General request leads with its REQ-#### container number.
    public static string PrimaryReference(Request record) =>
        record.Kind != RequestType.General && !string.IsNullOrWhiteSpace(record.Reference)
            ? record.Reference
            : string.IsNullOrEmpty(record.DisplayNumber) ? record.Reference : record.DisplayNumber;

    public static bool ShowsContainerNumber(Request record) =>
        !string.IsNullOrEmpty(record.DisplayNumber) && record.DisplayNumber != PrimaryReference(record);

    public static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    public static string Date(DateTimeOffset? value) =>
        value is null ? "—" : value.Value.LocalDateTime.ToString("d MMM yyyy");

    public static string StatusClass(RequestStatus status) => status switch
    {
        RequestStatus.NeedsAction    => "bg-amber-500/10 text-amber-600",
        RequestStatus.Open           => "bg-blue-500/10 text-blue-600",
        RequestStatus.NeedsVariation => "bg-violet-500/10 text-violet-600",
        RequestStatus.Closed         => "bg-surface-raised border border-line text-content-muted",
        _                            => "bg-surface-raised border border-line text-content-muted"
    };
}
