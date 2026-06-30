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
