using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

/// <summary>
/// The record-less "internal communication" tags — Jewel's own staff-to-staff mail that belongs to
/// no to-do, bid package or work order: the director's build-ups to the site manager, a site
/// instruction, a note on a spec (Nigel, 2026-08-22 — the Internal tab had only to-dos to offer
/// such an email). The same shape as <see cref="SubcontractorComms"/>: one GENERAL record plus
/// CATEGORY records, each a constant virtual record rather than a table row; the tag says WHAT the
/// thread is. They travel the ordinary record-link path under the InternalComms record type, file
/// the thread under the Internal pathway, and read back live on Internal → Communications.
/// </summary>
public static class InternalComms
{
    public const string RecordId = "internal-comms";
    public const string Reference = "IntComms";
    public const string Tag = "JPMS/IntComms";

    public static LinkableRecord Record { get; } = Define(
        RecordId, Reference,
        "Internal communication",
        "General staff-to-staff correspondence not tied to a record");

    public static IReadOnlyList<LinkableRecord> Categories { get; } = new[]
    {
        Define("internal-comms-site-instruction", "IntComms-Site", "Site instruction",
            "An instruction to site — what to do, where, by when"),
        Define("internal-comms-build-up", "IntComms-BuildUp", "Build-up",
            "Build-ups and construction detail for the site team — walls, ceilings, floors, finishes"),
        Define("internal-comms-spec-note", "IntComms-Spec", "Spec note",
            "A note on a specification, product or material choice"),
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
        Type: RecordType.InternalComms,
        RecordId: recordId,
        ProjectId: "",
        Reference: reference,
        TagReference: reference,
        Title: title,
        StatusLabel: null,
        Summary: summary);
}
