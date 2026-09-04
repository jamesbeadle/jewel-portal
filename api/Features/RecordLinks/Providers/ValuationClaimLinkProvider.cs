using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for valuation claims — the live report's periods ("August 2026").
// The claim is where a valuation's correspondence gathers BEFORE anything is put to the client;
// the client-facing statement stays the frozen snapshot (ValuationReportSnapshotLinkProvider),
// and a snapshot reads its claim's mail alongside its own, so everything tagged to the period
// travels with the statement frozen from it. Confirming and rolling over mints the next claim
// number and with it the next tag. Client-side by construction (TriageCategories.BucketFor).
//
// The stem comes from the per-project ClaimNumber (stable — the period name is renameable),
// project-qualified like the snapshot's because JPMS tags share one flat mailbox-category space:
//   TagReference = "VAL-{projectRef}-{ClaimNumber}"  ->  category "JPMS/VAL-{projectRef}-{ClaimNumber}".
public sealed class ValuationClaimLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private const string Prefix = "VAL";
    private readonly JpmsContext context;

    public ValuationClaimLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.ValuationClaim;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { Prefix };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken cancellationToken)
    {
        var projectReference = await ProjectReferenceAsync(projectId, cancellationToken);
        // Newest first — the live period leads; confirmed ones follow for the late reply.
        var claims = await context.ValuationClaims.AsNoTracking()
            .Where(claim => claim.ProjectId == projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .ToListAsync(cancellationToken);
        return claims
            .Select(claim => ToLinkable(projectReference, claim))
            .ToList()
            .AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ValuationClaimId == recordId, cancellationToken);
        if (claim is null) return null;
        return ToLinkable(await ProjectReferenceAsync(claim.ProjectId, cancellationToken), claim);
    }

    // "VAL-{projectRef}-{number}" back to its claim: the number is the last segment, and every
    // candidate carrying it is checked against its own full stem (two projects' claim 20 differ there).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken cancellationToken)
    {
        if (!tagReference.StartsWith(Prefix + "-", StringComparison.OrdinalIgnoreCase)) return null;
        var numberStart = tagReference.LastIndexOf('-') + 1;
        if (!int.TryParse(tagReference[numberStart..], out var claimNumber) || claimNumber <= 0) return null;

        var candidates = await context.ValuationClaims.AsNoTracking()
            .Where(claim => claim.ClaimNumber == claimNumber)
            .ToListAsync(cancellationToken);
        foreach (var claim in candidates)
        {
            var record = ToLinkable(await ProjectReferenceAsync(claim.ProjectId, cancellationToken), claim);
            if (record.TagReference.Equals(tagReference, StringComparison.OrdinalIgnoreCase)) return record;
        }
        return null;
    }

    private async Task<string> ProjectReferenceAsync(string projectId, CancellationToken cancellationToken)
    {
        var reference = await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == projectId)
            .Select(project => project.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        // No human reference yet → the (unique) project id, as the cost-centre and snapshot providers do.
        return string.IsNullOrWhiteSpace(reference) ? projectId : reference.Trim();
    }

    private static LinkableRecord ToLinkable(string projectReference, ValuationClaimEntity claim)
    {
        var reference = $"{Prefix}-{projectReference}-{claim.ClaimNumber}";
        var status = (ValuationClaimStatus)claim.Status;
        return new LinkableRecord(
            Type:         RecordType.ValuationClaim,
            RecordId:     claim.ValuationClaimId,
            ProjectId:    claim.ProjectId,
            Reference:    reference,
            TagReference: reference,
            // Led by the period's own name, the name a triager knows the claim by — the VAL stem
            // is a mail tag nobody recognises, as with snapshots (decision 2026-08-20).
            Title:        DisplayNameFor(claim),
            StatusLabel:  status.DisplayName(),
            Summary:      $"Claim {claim.ClaimNumber} — dated {claim.ClaimDate:dd MMM yyyy}",
            // A confirmed period is finished work: still linkable (a late reply belongs to the
            // period it answers), but the live period is what a picker leads with.
            IsActive:     status != ValuationClaimStatus.Confirmed);
    }

    // "August 2026" when named, otherwise "Claim 20" — the one rule ValuationClaim.DisplayName keeps.
    private static string DisplayNameFor(ValuationClaimEntity claim) =>
        string.IsNullOrWhiteSpace(claim.Name) ? $"Claim {claim.ClaimNumber}" : claim.Name.Trim();
}
