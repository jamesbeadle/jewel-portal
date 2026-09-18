using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Builds the project-qualified mailbox tag stem for a frozen valuation statement, the way
/// RequestTags and WorkOrderTags do for theirs:
///   stem = "VRS-{projectRef}-{number}"  ->  category "JPMS/VRS-JBB-2026-001-4".
///
/// A snapshot has no reference of its own — a GUID and a free-text label — so the stem is minted
/// from the per-project sequential Number stamped at capture. It was minted in ONE place, at read
/// time inside the link provider, which is why the outgoing statement carried the Client pathway
/// and no record tag: the handler had nowhere to ask what the snapshot's tag was, and a tag
/// spelt a second way would be a tag the link provider does not recognise, which is worse than
/// none. Both read this now, so the statement's sent copy and the client's reply file themselves
/// under the statement they are about.
/// </summary>
internal static class ValuationSnapshotTags
{
    /// <summary>The stem from an already-resolved project reference. The one place it is spelt.</summary>
    public static string Stem(string projectReference, int number) => $"VRS-{projectReference}-{number}";

    /// <summary>The stem for one snapshot, resolving its project's reference — falling back to the
    /// (unique) project id when the project has no human reference yet, so the stem stays
    /// project-unique either way, exactly as the register's own reading does.</summary>
    public static async Task<string> StemAsync(
        JpmsContext context, string projectId, int number, CancellationToken cancellationToken)
    {
        var projectReference = await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == projectId)
            .Select(project => project.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        return Stem(string.IsNullOrWhiteSpace(projectReference) ? projectId : projectReference.Trim(), number);
    }
}
