using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for valuation-report cost centres. Unlike the Request / Bid Package
// providers, a cost centre isn't a per-project row: the CostCenters table is the global master
// (Code + Name), referenced by every project's Financials tab. So a linkable "record" here is the
// pairing of a project and a cost centre, and an email tagged to it surfaces when you click through
// to that cost centre on that project.
//
// Because the cost-centre codes repeat across projects (every project has "0001"), and JPMS tags
// share one flat mailbox-category space, the tag stem MUST carry a project qualifier — otherwise
// project A's "0001" mail would bleed into project B's "0001". Hence the "project-cost centre" tag:
//   TagReference = "CC-{projectRef}-{code}"  ->  category "JPMS/CC-{projectRef}-{code}".
//
// The RecordId is the composite "{projectId}::{costCentreId}" so FindAsync can resolve both halves
// (the read/link layer only hands back a RecordId, never the project separately).
public sealed class CostCentreLinkProvider : ILinkableRecordProvider
{
    private const string IdSeparator = "::";

    private readonly JpmsContext context;

    public CostCentreLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.CostCentre;

    // Cost-centre links own the "CC" reference namespace (tags are "CC-<projectRef>-<code>").
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "CC" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var projectRef = await ProjectRefAsync(projectId, ct);

        // The cost-centre master is a shared hierarchy referenced by every project's Financials tab,
        // so every active cost centre is linkable for this project. Ordered as the Financials tab shows.
        var centres = await context.CostCenters.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return centres.Select(c => ToLinkable(projectId, projectRef, c)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var (projectId, costCentreId) = SplitId(recordId);
        if (projectId is null || costCentreId is null) return null;

        var centre = await context.CostCenters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CostCenterId == costCentreId, ct);
        if (centre is null) return null;

        var projectRef = await ProjectRefAsync(projectId, ct);
        return ToLinkable(projectId, projectRef, centre);
    }

    private async Task<string> ProjectRefAsync(string projectId, CancellationToken ct)
    {
        var reference = await context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(ct);
        // Fall back to the (unique) project id if the project has no human reference yet, so the tag
        // stem stays project-unique either way.
        return string.IsNullOrWhiteSpace(reference) ? projectId : reference.Trim();
    }

    private static LinkableRecord ToLinkable(string projectId, string projectRef, CostCenterEntity centre)
    {
        var code = centre.Code.Trim();
        return new LinkableRecord(
            Type:         RecordType.CostCentre,
            RecordId:     $"{projectId}{IdSeparator}{centre.CostCenterId}",
            ProjectId:    projectId,
            Reference:    code,                                   // shown in mono in the picker, e.g. "0001"
            TagReference: $"CC-{projectRef}-{code}",              // project-qualified, flat-space-unique
            Title:        centre.Name,                            // e.g. "Contract Works"
            StatusLabel:  null);
    }

    private static (string? projectId, string? costCentreId) SplitId(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId)) return (null, null);
        var idx = recordId.IndexOf(IdSeparator, StringComparison.Ordinal);
        if (idx <= 0 || idx + IdSeparator.Length >= recordId.Length) return (null, null);
        return (recordId[..idx], recordId[(idx + IdSeparator.Length)..]);
    }
}
