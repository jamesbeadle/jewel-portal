using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for a project's Scheduling bucket. Unlike Requests / Bid Package Invites
// there is no per-item table behind this: every project has exactly ONE scheduling record — a bucket
// that collects programme/scheduling correspondence for the project's Schedule tab. So FindAsync
// resolves a single LinkableRecord and the RecordId is simply the project id.
//
// JPMS tags share one flat mailbox-category space, so the tag stem is project-qualified the same way
// cost-centre tags are:
//   TagReference = "SCH-{projectRef}"  ->  category "JPMS/SCH-{projectRef}".
//
// ForProjectAsync goes deeper than the bucket: alongside it, the picker lists the project's claims
// documents — the NOD/EOT requests (owned by RequestLinkProvider) and the LADs claims (owned by
// LadLinkProvider) — so a triage email can be linked to the specific scheduling document it concerns
// rather than only the general bucket. Each of those records carries its OWN Type/RecordId, so the
// link and read paths still resolve through the owning provider; nothing here changes the tag layer.
//
// The Schedule tab's Communications view reads the bucket's emails back live by tag
// (RecordEmailReader), identically to every other record type. The bucket itself is link-only:
// nothing is ever created from an email — it exists implicitly for every project.
public sealed class SchedulingLinkProvider : ILinkableRecordProvider
{
    private static readonly int[] ClaimRequestKinds =
    {
        (int)RequestType.NoticeOfDelay,
        (int)RequestType.ExtensionOfTime
    };

    private readonly JpmsContext context;

    public SchedulingLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Scheduling;

    // Scheduling links own the "SCH" reference namespace (tags are "SCH-<projectRef>"). The claim
    // records listed alongside the bucket keep their owning providers' namespaces (NOD/EOT/LAD).
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "SCH" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var bucket = await FindAsync(projectId, ct);
        if (bucket is null) return Array.Empty<LinkableRecord>();

        var records = new List<LinkableRecord> { bucket };

        // The schedule's claims documents: Jewel's own NOD/EOT notices (Request family) …
        var projectRef = await RequestTags.ProjectRefAsync(context, projectId, ct);
        // Closed claims documents are excluded for the same reason RequestLinkProvider excludes
        // closed requests: triage links emails to live records only.
        var claimRequests = await context.Requests.AsNoTracking()
            .Where(r => r.ProjectId == projectId && ClaimRequestKinds.Contains(r.Kind)
                        && r.Status != (int)RequestStatus.Closed)
            .OrderByDescending(r => r.RaisedAt)
            .ToListAsync(ct);
        records.AddRange(claimRequests.Select(r => new LinkableRecord(
            Type:         RecordType.Request,
            RecordId:     r.RequestId,
            ProjectId:    r.ProjectId,
            Reference:    string.IsNullOrWhiteSpace(r.Reference) ? r.TagReference : r.Reference.Trim(),
            TagReference: RequestTags.Stem(projectRef, r.ProjectId, r.TagReference),
            Title:        r.Title,
            StatusLabel:  ((RequestStatus)r.Status).ToString(),
            Summary:      RecordSummaries.Clip(r.Description))));

        // … and the client's LADs claims against Jewel.
        var ladClaims = await context.LadClaims.AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.Number)
            .ToListAsync(ct);
        records.AddRange(ladClaims.Select(l => new LinkableRecord(
            Type:         RecordType.Lad,
            RecordId:     l.LadClaimId,
            ProjectId:    l.ProjectId,
            Reference:    l.Reference,
            TagReference: l.Reference,
            Title:        l.Title,
            StatusLabel:  ((LadStatus)l.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(l.Description))));

        return records;
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
            Title:        "Programme communications",
            StatusLabel:  null);
    }
}
