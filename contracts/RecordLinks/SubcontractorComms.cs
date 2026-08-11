using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

/// <summary>
/// The one "subcontractor communication" record: general correspondence with a subcontractor that
/// belongs to no bid package, work order or defect. Deliberately a single record rather than one
/// per company (decision 2026-08-10) — the tag says WHAT the thread is, and which company it is
/// with is plain from the thread itself. It travels the ordinary record-link path, so linking it
/// files the thread under the Subcontractor pathway like every other subcontract-side record, and
/// the Subcontractor → Communications page reads the mail back live by the tag.
/// </summary>
public static class SubcontractorComms
{
    /// <summary>The virtual record's id — constant, because there is exactly one.</summary>
    public const string RecordId = "subcontractor-comms";

    /// <summary>The reference / tag stem: the mailbox category is "JPMS/SubComms".</summary>
    public const string Reference = "SubComms";

    /// <summary>The full mailbox category, for reading the communications back.</summary>
    public const string Tag = "JPMS/SubComms";

    /// <summary>The record as the link layer sees it — the client stages it, the server serves it,
    /// so the two can never disagree about what is being tagged.</summary>
    public static LinkableRecord Record { get; } = new(
        Type: RecordType.SubcontractorComms,
        RecordId: RecordId,
        ProjectId: "",
        Reference: Reference,
        TagReference: Reference,
        Title: "Subcontractor communication",
        StatusLabel: null,
        Summary: "General subcontractor correspondence not tied to a record");
}
