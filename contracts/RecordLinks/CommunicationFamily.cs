using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

/// <summary>
/// One record-less communication tag family as the UI reads it — the Subcontractor one and the
/// Internal one share every behaviour (a general tick, category ticks, a live-read page with
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

    public static IReadOnlyList<CommunicationFamily> Known { get; } = new[] { Subcontractor, Internal };

    /// <summary>The family a communications page serves, from its route; Subcontractor when unknown.</summary>
    public static CommunicationFamily ForRoute(string path) =>
        Known.FirstOrDefault(family => path.TrimEnd('/').EndsWith(family.Route, StringComparison.OrdinalIgnoreCase))
        ?? Subcontractor;
}
