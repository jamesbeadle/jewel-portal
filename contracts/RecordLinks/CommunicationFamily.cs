using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

/// <summary>
/// One record-less communication tag family as the UI reads it — the Subcontractor, Supplier and
/// Internal ones share every behaviour (a general tick, category ticks, a live-read page with
/// category chips), so the pages and panes take the family as data rather than being written twice.
/// </summary>
public sealed record CommunicationFamily(
    string Label,
    string Pathway,
    string Route,
    LinkableRecord General,
    IReadOnlyList<LinkableRecord> Categories,
    IReadOnlyList<LinkableRecord> All,
    IReadOnlyList<string> Tags,
    string GeneralHint)
{
    public string Tag => $"JPMS/{General.TagReference}";

    public static string TagFor(LinkableRecord record) => $"JPMS/{record.TagReference}";

    public string ChipLabel(LinkableRecord record) => record.RecordId == General.RecordId ? "General" : record.Title;

    public static CommunicationFamily Subcontractor { get; } = new(
        "Subcontractor communication",
        "Subcontractor",
        "/subcontractors/communications",
        SubcontractorComms.Record,
        SubcontractorComms.Categories,
        SubcontractorComms.All,
        SubcontractorComms.Tags,
        "Tags the thread as general subcontractor correspondence — no record needed. It appears under Subcontractor → Communications.");

    public static CommunicationFamily Internal { get; } = new(
        "Internal communication",
        "Internal",
        "/internal/communications",
        InternalComms.Record,
        InternalComms.Categories,
        InternalComms.All,
        InternalComms.Tags,
        "Tags the thread as staff-to-staff correspondence — no record needed. It appears under Internal → Communications.");

    public static CommunicationFamily Supplier { get; } = new(
        "Supplier communication",
        "Supplier",
        "/suppliers/communications",
        SupplierComms.Record,
        SupplierComms.Categories,
        SupplierComms.All,
        SupplierComms.Tags,
        "Tags the thread as general supplier correspondence — no record needed. It appears under Supplier → Communications.");

    public static IReadOnlyList<CommunicationFamily> Known { get; } = new[] { Subcontractor, Supplier, Internal };

    /// <summary>URL slug for one of the family's records ("Chaser" → "chaser", "H&amp;S" → "h-s")
    /// — the category segment of the register deep links ("/subcontractors/communications/chaser").</summary>
    public static string Slug(LinkableRecord record) =>
        string.Join('-',
            record.Title.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
                .Aggregate("", (acc, c) => acc + c)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The register page for one of the family's records — the family route for the
    /// general record, a category deep link for a category.</summary>
    public string RouteFor(LinkableRecord record) =>
        record.RecordId == General.RecordId ? Route : $"{Route}/{Slug(record)}";

    /// <summary>Resolve a register deep link's category segment back to the family record it
    /// names, or null (the whole family) when the segment is blank or unknown.</summary>
    public LinkableRecord? ForSlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : Categories.FirstOrDefault(record => Slug(record).Equals(slug.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>The family a communications page serves, from its route; Subcontractor when unknown.
    /// Category deep links ("…/communications/chaser") resolve to their family too.</summary>
    public static CommunicationFamily ForRoute(string path) =>
        Known.FirstOrDefault(family =>
            path.TrimEnd('/').EndsWith(family.Route, StringComparison.OrdinalIgnoreCase)
            || path.Contains(family.Route + "/", StringComparison.OrdinalIgnoreCase))
        ?? Subcontractor;
}
