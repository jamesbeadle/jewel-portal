using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

/// <summary>
/// The record-less "subcontractor communication" tags: correspondence with a subcontractor that
/// belongs to no bid package, work order or defect. One GENERAL record plus a small set of CATEGORY
/// records (decision 2026-08-17 — "more than just comms"): Chaser, Info request, H&amp;S (Materials moved to <see cref="SupplierComms"/> 2026-08-27).
/// Defects deliberately have no category — defect chasing files against the project's actual Defect
/// records. Each is a constant virtual record rather than a table row (decision 2026-08-10): the tag
/// says WHAT the thread is, and which company it is with is plain from the thread itself. They all
/// travel the ordinary record-link path under the one SubcontractorComms record type, so linking any
/// of them files the thread under the Subcontractor pathway, and the Subcontractor → Communications
/// page (and its workspace pane) reads the mail back live by these tags, filterable per category.
/// </summary>
public static class SubcontractorComms
{
    /// <summary>The general record's id — constant, because there is exactly one.</summary>
    public const string RecordId = "subcontractor-comms";

    /// <summary>The general tag stem: the mailbox category is "JPMS/SubComms". Every category stem
    /// extends this ("SubComms-Chase"), so the whole family shares one reference prefix.</summary>
    public const string Reference = "SubComms";

    /// <summary>The general record's full mailbox category.</summary>
    public const string Tag = "JPMS/SubComms";

    /// <summary>The general record — correspondence that fits no category.</summary>
    public static LinkableRecord Record { get; } = Define(
        RecordId, Reference,
        "Subcontractor communication",
        "General subcontractor correspondence not tied to a record");

    /// <summary>The category records — more specific flavours of the same record-less tag.</summary>
    public static IReadOnlyList<LinkableRecord> Categories { get; } = new[]
    {
        Define("subcontractor-comms-chase", "SubComms-Chase", "Chaser",
            "Chasing a subcontractor — outstanding information, attendance or progress"),
        Define("subcontractor-comms-info", "SubComms-Info", "Info request",
            "Information asked of the subcontractor, or sent to them"),
        // Materials moved to the SUPPLIER family 2026-08-27 ("materials are mainly supplier") —
        // see SupplierComms, which keeps the record id and SubComms-Mats tag stem.
        Define("subcontractor-comms-hs", "SubComms-HS", "H&S",
            "Health and safety correspondence"),
    };

    /// <summary>General first, then the categories — everything the link provider serves and the
    /// Communications views offer as filters.</summary>
    public static IReadOnlyList<LinkableRecord> All { get; } = BuildAll();

    /// <summary>Every full mailbox category in the family, for reading the communications back.</summary>
    public static IReadOnlyList<string> Tags { get; } =
        All
            .Select(record => $"JPMS/{record.TagReference}")
            .ToList();

    /// <summary>Resolve one of the family's records by id, or null — the provider's FindAsync.</summary>
    public static LinkableRecord? Find(string recordId) =>
        All
            .FirstOrDefault(record => record.RecordId == recordId);

    private static IReadOnlyList<LinkableRecord> BuildAll()
    {
        var all = new List<LinkableRecord> { Record };
        all.AddRange(Categories);
        return all;
    }

    private static LinkableRecord Define(string recordId, string reference, string title, string summary) => new(
        Type: RecordType.SubcontractorComms,
        RecordId: recordId,
        ProjectId: "",
        Reference: reference,
        TagReference: reference,
        Title: title,
        StatusLabel: null,
        Summary: summary);
}
