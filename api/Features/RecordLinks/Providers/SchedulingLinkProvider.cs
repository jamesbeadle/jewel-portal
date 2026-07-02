using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for a project's Scheduling bucket. Unlike Requests / Bid Package Invites
// there is no per-item table behind this: every project has exactly ONE scheduling record — a bucket
// that collects programme/scheduling correspondence for the project's Schedule tab. So ForProjectAsync
// returns a single LinkableRecord and the RecordId is simply the project id.
//
// JPMS tags share one flat mailbox-category space, so the tag stem is project-qualified the same way
// cost-centre tags are:
//   TagReference = "SCH-{projectRef}"  ->  category "JPMS/SCH-{projectRef}".
//
// The Schedule tab's Communications view reads these emails back live by that tag (RecordEmailReader),
// identically to every other record type. Scheduling records are link-only: nothing is ever created
// from an email — the bucket exists implicitly for every project.
public sealed class SchedulingLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public SchedulingLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Scheduling;

    // Scheduling links own the "SCH" reference namespace (tags are "SCH-<projectRef>").
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "SCH" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var record = await FindAsync(projectId, ct);
        return record is null
            ? Array.Empty<LinkableRecord>()
            : new[] { record };
    }

    // The RecordId IS the project id — one scheduling bucket per project.
    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var project = await context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == recordId)
            .Select(p => new { p.ProjectId, p.Reference })
            .FirstOrDefaultAsync(ct);
        if (project is null) return null;

        // Fall back to the (unique) project id if the project has no human reference yet, so the tag
        // stem stays project-unique either way — same rule as the cost-centre provider.
        var projectRef = string.IsNullOrWhiteSpace(project.Reference) ? project.ProjectId : project.Reference.Trim();
        var reference = $"SCH-{projectRef}";

        return new LinkableRecord(
            Type:         RecordType.Scheduling,
            RecordId:     project.ProjectId,
            ProjectId:    project.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        "Schedule communications",
            StatusLabel:  null);
    }
}
