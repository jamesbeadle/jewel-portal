using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Features.Triage;

/// <summary>The records a thread's existing tags name: which resolved record answers for which
/// stem, and how one reads to a person — the one rule behind the triage bar's tag chips and
/// the apply's "every stem must match" check.</summary>
public static class ThreadTagRecords
{
    /// <summary>A resolved record answers for its stem as written (the qualified stem, or a
    /// global one like TODO-0011), for the reference people say, and for a legacy bare stem the
    /// qualified record was the only match for ("WO-0048" → "JBB-2026-001-WO-0048").</summary>
    public static bool Names(LinkableRecord record, string stem) =>
        record.TagReference.Equals(stem, StringComparison.OrdinalIgnoreCase)
        || record.Reference.Equals(stem, StringComparison.OrdinalIgnoreCase)
        || record.TagReference.EndsWith("-" + stem, StringComparison.OrdinalIgnoreCase);

    /// <summary>The record a mailbox tag ("JPMS/INV-0021" or its bare stem) resolved to, or null
    /// when nothing did — an unresolved tag renders as plain text, never an error.</summary>
    public static LinkableRecord? Find(IReadOnlyList<LinkableRecord> records, string tag)
    {
        var stem = TriageEmailDisplay.TagLabel(tag);
        return records.FirstOrDefault(record => Names(record, stem));
    }

    /// <summary>What the record is, in words: "INV-0021 · Copper pipe 15mm — Inventory item on
    /// By France (In stock)". The reference alone tells a triager nothing.</summary>
    public static string Describe(LinkableRecord record, string projectName)
    {
        var what = PathwayPaneConfig.TypeLabel(record.Type);
        var where = string.IsNullOrWhiteSpace(projectName) ? "" : $" on {projectName}";
        var status = string.IsNullOrWhiteSpace(record.StatusLabel) ? "" : $" ({record.StatusLabel})";
        var title = string.IsNullOrWhiteSpace(record.Title) ? "" : $" · {record.Title}";
        return $"{record.Reference}{title} — {what}{where}{status}";
    }
}
