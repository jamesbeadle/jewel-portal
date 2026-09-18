using Jewel.JPMS.Api.Features.Commercial;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// The RETIRED valuation-report-snapshot record type (RecordType.ValuationReportSnapshot),
// consolidated into the claim on 2026-09-18. This provider exists so the old world keeps
// resolving, not so anything new is filed to it:
//   - a snapshot id in a saved link, an old page, or a connector call (file_email_to_record /
//     read_record_emails with type ValuationReportSnapshot) resolves to the CLAIM it was frozen
//     from — FindAsync returns the claim's own LinkableRecord (type ValuationClaim, tag VAL-…),
//     so a filing lands on the claim and a read shows the claim's mail plus its retired stems;
//   - a "VRS-{projectRef}-{n}" stem on old mail resolves back to that claim (the claim provider
//     does the parsing; this one owns the prefix so the namespace check stays honest).
// ForProjectAsync lists nothing: the picker's one row per period is the claim.
public sealed class RetiredValuationStatementLinkProvider : ILinkableRecordProvider, ITagResolvingProvider, ICompanionRecordProvider
{
    private readonly JpmsContext context;
    private readonly ValuationClaimLinkProvider claims;

    public RetiredValuationStatementLinkProvider(JpmsContext context)
    {
        this.context = context;
        claims = new ValuationClaimLinkProvider(context);
    }

    public RecordType Type => RecordType.ValuationReportSnapshot;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { ValuationClaimTags.RetiredStatementPrefix };

    public Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<LinkableRecord>>(Array.Empty<LinkableRecord>());

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var claimId = await context.ValuationClaimLegacyStatements.AsNoTracking()
            .Where(alias => alias.ValuationReportSnapshotId == recordId)
            .Select(alias => alias.ValuationClaimId)
            .FirstOrDefaultAsync(ct);
        // A claim id passed under the retired type is honoured too — one less way to be wrong.
        return await claims.FindAsync(claimId ?? recordId, ct);
    }

    public Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct) =>
        tagReference.StartsWith(ValuationClaimTags.RetiredStatementPrefix + "-", StringComparison.OrdinalIgnoreCase)
            ? claims.FindByTagAsync(tagReference, ct)
            : Task.FromResult<LinkableRecord?>(null);

    public Task<IReadOnlyList<string>> CompanionTagReferencesAsync(LinkableRecord record, CancellationToken ct) =>
        claims.CompanionTagReferencesAsync(record, ct);
}
