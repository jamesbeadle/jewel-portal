using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// One provider per record type. This is the single seam the record-agnostic link layer (the generic
// link command, the list query, and RecordEmailReader) talks to, so adding a new linkable record type
// is "implement this interface + register it" — no changes to the link/read code or the triage UI.
//
// A provider knows how to (a) list its records for a project as LinkableRecords and (b) resolve one
// record by id. It deliberately does NOT do the tagging itself: the tag write/read is shared and lives
// in the graph client, keyed off LinkableRecord.TagReference, identically for every record type.
public interface ILinkableRecordProvider
{
    // The record type this provider serves. The registry maps on this.
    RecordType Type { get; }

    // The reference prefix this provider owns (e.g. "RFI"/"RFA"/… via the request kinds, "BPI"). Used
    // to assert reference namespaces don't collide across types, since all tags share one flat space.
    IReadOnlyCollection<string> ReferencePrefixes { get; }

    // All records of this type on the project, projected for the triage picker.
    Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct);

    // Resolve a single record (for linking + reading its mail), or null if it no longer exists.
    Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct);
}

// The REVERSE lookup: a mailbox tag stem ("TODO-0011", "JBB-2026-001-RFI-012") back to the record
// it names. Optional — a provider implements this only when its tag grammar supports it, and
// ResolveRecordTagsHandler simply skips the rest, so a stem from an unimplemented family renders
// as a plain (unlinked) chip rather than failing. Each implementation must recognise its OWN
// grammar cheaply (a prefix/shape check) before touching the database: the handler offers every
// stem to every implementing provider, first non-null answer wins.
public interface ITagResolvingProvider
{
    // The record a stem names, or null when the stem isn't this provider's shape or names nothing.
    Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct);
}

// Shared parse for the simple global-sequence stems ("TODO-0011" -> 11). Project-qualified
// families (requests, variations) carry their own grammar in their providers instead.
internal static class TagReferenceParsing
{
    public static bool TryParseNumber(string tagReference, string prefix, out int number)
    {
        number = 0;
        return tagReference.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(tagReference[(prefix.Length + 1)..], out number)
            && number > 0;
    }
}
