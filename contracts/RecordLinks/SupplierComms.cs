using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

/// <summary>
/// The record-less "supplier communication" tags — correspondence with a materials/goods supplier,
/// as distinct from a subcontractor (decision 2026-08-27: the Control Centre's pathway restructure
/// gave suppliers their own side). The same shape as <see cref="SubcontractorComms"/>: one GENERAL
/// record plus CATEGORY records, each a constant virtual record rather than a table row; the tag
/// says WHAT the thread is. They travel the ordinary record-link path under the SupplierComms
/// record type, file the thread under the Supplier pathway, and read back live on
/// Supplier → Communications, filterable per category.
///
/// Materials moved here FROM the subcontractor family (Nigel, 2026-08-27: "materials are mainly
/// supplier"). Its record id and "SubComms-Mats" tag stem are persisted identifiers and survive
/// the move (VOQ precedent), so every email already tagged JPMS/SubComms-Mats reads back in the
/// Supplier → Materials register with no migration — only the record's family (and therefore the
/// pathway it files under) changed.
/// </summary>
public static class SupplierComms
{
    public const string RecordId = "supplier-comms";
    public const string Reference = "SupComms";
    public const string Tag = "JPMS/SupComms";

    public static LinkableRecord Record { get; } = Define(
        RecordId, Reference,
        "Supplier communication",
        "General supplier correspondence not tied to a record");

    public static IReadOnlyList<LinkableRecord> Categories { get; } = new[]
    {
        // Re-homed from SubcontractorComms 2026-08-27 — id and tag stem retained (see class doc).
        Define("subcontractor-comms-materials", "SubComms-Mats", "Materials",
            "Materials — orders, deliveries, availability"),
    };

    public static IReadOnlyList<LinkableRecord> All { get; } = BuildAll();

    public static IReadOnlyList<string> Tags { get; } =
        All
            .Select(record => $"JPMS/{record.TagReference}")
            .ToList();

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
        Type: RecordType.SupplierComms,
        RecordId: recordId,
        ProjectId: "",
        Reference: reference,
        TagReference: reference,
        Title: title,
        StatusLabel: null,
        Summary: summary);
}
