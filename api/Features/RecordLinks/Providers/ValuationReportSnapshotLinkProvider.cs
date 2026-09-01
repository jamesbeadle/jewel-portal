using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for valuation report snapshots — the frozen client-facing statements
// on the project's Valuation Snapshots register. A triage email (the client's response to a
// submission, the architect's queries on a period-end statement) links to the SPECIFIC snapshot
// it concerns, and the snapshot viewer reads its mail back live by tag (RecordEmailReader) —
// the same mechanism every other record family uses, with no changes to the link/read layer.
//
// Client-side by construction: the snapshot is the only client-facing form of the valuation
// report (decision 2026-07-22), so TriageCategories.BucketFor maps the type to JPMS/Client —
// snapshot correspondence can never share a thread with subcontractor or internal mail.
//
// Snapshots have no reference of their own (a GUID id and a free-text label), so the tag stem
// is minted from the per-project sequential Number stamped at capture, project-qualified the
// same way cost-centre and scheduling tags are (JPMS tags share one flat mailbox-category
// space, and every project's register counts from 1):
//   TagReference = "VRS-{projectRef}-{Number}"  ->  category "JPMS/VRS-{projectRef}-{Number}".
public sealed class ValuationReportSnapshotLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public ValuationReportSnapshotLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.ValuationReportSnapshot;

    // Snapshot links own the "VRS" reference namespace (tags are "VRS-<projectRef>-<number>").
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "VRS" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var projectRef = await ProjectRefAsync(projectId, ct);

        // Newest first — the snapshot an email is about is almost always the latest one.
        var snapshots = await context.ValuationReportSnapshots.AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.TakenAt)
            .ToListAsync(ct);

        var claimNames = await ClaimNamesAsync(snapshots.Select(s => s.ValuationClaimId), ct);
        return snapshots.Select(s => ToLinkable(projectRef, s, ClaimNameFor(claimNames, s))).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var snapshot = await context.ValuationReportSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ValuationReportSnapshotId == recordId, ct);
        if (snapshot is null) return null;

        var projectRef = await ProjectRefAsync(snapshot.ProjectId, ct);
        var claimNames = await ClaimNamesAsync(new[] { snapshot.ValuationClaimId }, ct);
        return ToLinkable(projectRef, snapshot, ClaimNameFor(claimNames, snapshot));
    }

    // The display names of the claims the snapshots were frozen from, keyed by claim id — the
    // valuation's own name ("Valuation 14", "June 2026"), or the register's "Claim n" fallback
    // for pre-name claims (the same one rule as ValuationClaim.DisplayName).
    private async Task<Dictionary<string, string>> ClaimNamesAsync(IEnumerable<string?> claimIds, CancellationToken ct)
    {
        var ids = claimIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string>();
        var claims = await context.ValuationClaims.AsNoTracking()
            .Where(c => ids.Contains(c.ValuationClaimId))
            .Select(c => new { c.ValuationClaimId, c.Name, c.ClaimNumber })
            .ToListAsync(ct);
        return claims.ToDictionary(
            c => c.ValuationClaimId,
            c => string.IsNullOrWhiteSpace(c.Name) ? $"Claim {c.ClaimNumber}" : c.Name.Trim());
    }

    private static string? ClaimNameFor(Dictionary<string, string> claimNames, ValuationReportSnapshotEntity snapshot) =>
        snapshot.ValuationClaimId is { } claimId && claimNames.TryGetValue(claimId, out var name) ? name : null;

    private async Task<string> ProjectRefAsync(string projectId, CancellationToken ct)
    {
        var reference = await context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(ct);
        // Fall back to the (unique) project id if the project has no human reference yet, so the
        // tag stem stays project-unique either way — same rule as the cost-centre provider.
        return string.IsNullOrWhiteSpace(reference) ? projectId : reference.Trim();
    }

    private static LinkableRecord ToLinkable(string projectRef, ValuationReportSnapshotEntity snapshot, string? claimName)
    {
        var reference = $"VRS-{projectRef}-{snapshot.Number}";
        var stage = string.IsNullOrWhiteSpace(snapshot.Label) ? "Valuation report snapshot" : snapshot.Label;
        return new LinkableRecord(
            Type:         RecordType.ValuationReportSnapshot,
            RecordId:     snapshot.ValuationReportSnapshotId,
            ProjectId:    snapshot.ProjectId,
            Reference:    reference,
            TagReference: reference,
            // Led by the valuation's own name — "Valuation 14 — VI-0004 raise" — because that is
            // the name a triager knows the report by (decision 2026-08-20); the capture label
            // alone ("VI-0004 raise") only says which invoice event froze it, and the VRS
            // reference is a mail-tag stem nobody recognises. Claim-less snapshots (none in
            // practice — every capture stamps the latest claim when one exists) fall back to
            // the label alone.
            Title:        claimName is null ? stage : $"{claimName} — {stage}",
            StatusLabel:  snapshot.IsSuperseded ? "Superseded" : null,
            Summary:      $"Taken {snapshot.TakenAt:dd MMM yyyy} — payment due £{snapshot.PaymentDueExVat:N2}",
            // Superseded rows stay linkable (a late reply about the statement that was actually
            // sent belongs on it) but pickers default to the live register.
            IsActive:     !snapshot.IsSuperseded);
    }
}
