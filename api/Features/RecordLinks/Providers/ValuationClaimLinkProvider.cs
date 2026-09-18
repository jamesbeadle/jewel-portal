using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for valuations — the periods of the valuation report ("August 2026").
// Since 2026-09-18 the claim is the ONE valuation object: its correspondence gathers here before
// anything is put to the client AND after — the locked claim is the statement the client is
// sent, so the sent copy and the client's reply file here too. Confirming and rolling over mints
// the next claim number and with it the next tag. Client-side by construction
// (TriageCategories.BucketFor).
//
// ForProjectAsync answers the picker's question — "what can a valuation email be filed to on
// this project?" — with one row per period, newest first. Superseded and retired "snapshot"
// rows no longer exist: mail tagged to a retired statement (JPMS/VRS-…) reads on its claim
// through the companion stems (ICompanionRecordProvider, merged in RecordEmailReader), so the
// Valuation Report's Correspondence and the connector's read_record_emails tell one story.
//
// The stem comes from the per-project ClaimNumber (stable — the period name is renameable),
// project-qualified because JPMS tags share one flat mailbox-category space:
//   TagReference = "VAL-{projectRef}-{ClaimNumber}"  ->  category "JPMS/VAL-{projectRef}-{ClaimNumber}".
public sealed class ValuationClaimLinkProvider : ILinkableRecordProvider, ITagResolvingProvider, ICompanionRecordProvider
{
    private readonly JpmsContext context;

    public ValuationClaimLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.ValuationClaim;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { ValuationClaimTags.Prefix };

    // Newest period first: the live period leads; confirmed ones follow for the late reply.
    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken cancellationToken)
    {
        var projectReference = await ValuationClaimTags.ProjectReferenceAsync(context, projectId, cancellationToken);
        var claims = await context.ValuationClaims.AsNoTracking()
            .Where(claim => claim.ProjectId == projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .ToListAsync(cancellationToken);
        return claims.Select(claim => ToLinkable(projectReference, claim)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ValuationClaimId == recordId, cancellationToken);
        if (claim is null) return null;
        return ToLinkable(await ValuationClaimTags.ProjectReferenceAsync(context, claim.ProjectId, cancellationToken), claim);
    }

    // "VAL-{projectRef}-{number}" back to its claim — and "VRS-{projectRef}-{number}", the retired
    // statement stem, back to the claim it was frozen from. The number is the last segment, and
    // every candidate carrying it is checked against its own full stem (two projects' claim 20
    // differ there).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken cancellationToken)
    {
        var numberStart = tagReference.LastIndexOf('-') + 1;
        if (!int.TryParse(tagReference[numberStart..], out var number) || number <= 0) return null;

        if (tagReference.StartsWith(ValuationClaimTags.Prefix + "-", StringComparison.OrdinalIgnoreCase))
        {
            var candidates = await context.ValuationClaims.AsNoTracking()
                .Where(claim => claim.ClaimNumber == number)
                .ToListAsync(cancellationToken);
            foreach (var claim in candidates)
            {
                var record = ToLinkable(await ValuationClaimTags.ProjectReferenceAsync(context, claim.ProjectId, cancellationToken), claim);
                if (record.TagReference.Equals(tagReference, StringComparison.OrdinalIgnoreCase)) return record;
            }
            return null;
        }

        if (tagReference.StartsWith(ValuationClaimTags.RetiredStatementPrefix + "-", StringComparison.OrdinalIgnoreCase))
        {
            var aliases = await context.ValuationClaimLegacyStatements.AsNoTracking()
                .Where(alias => alias.Number == number)
                .ToListAsync(cancellationToken);
            foreach (var alias in aliases)
            {
                var projectReference = await ValuationClaimTags.ProjectReferenceAsync(context, alias.ProjectId, cancellationToken);
                if (!ValuationClaimTags.RetiredStatementStem(projectReference, alias.Number)
                        .Equals(tagReference, StringComparison.OrdinalIgnoreCase)) continue;
                return await FindAsync(alias.ValuationClaimId, cancellationToken);
            }
        }
        return null;
    }

    // The period's companions are the retired statement stems frozen from it before 2026-09-18,
    // superseded ones included: the client's reply to a statement that was later re-issued is
    // still the period's correspondence. Empty for claims locked since.
    public async Task<IReadOnlyList<string>> CompanionTagReferencesAsync(LinkableRecord record, CancellationToken cancellationToken)
    {
        var numbers = await context.ValuationClaimLegacyStatements.AsNoTracking()
            .Where(alias => alias.ValuationClaimId == record.RecordId)
            .Select(alias => alias.Number)
            .ToListAsync(cancellationToken);
        if (numbers.Count == 0) return Array.Empty<string>();
        var projectReference = await ValuationClaimTags.ProjectReferenceAsync(context, record.ProjectId, cancellationToken);
        return numbers.Select(number => ValuationClaimTags.RetiredStatementStem(projectReference, number)).ToList();
    }

    internal static string Stem(string projectReference, int claimNumber) => ValuationClaimTags.Stem(projectReference, claimNumber);

    internal static LinkableRecord ToLinkable(string projectReference, ValuationClaimEntity claim)
    {
        var reference = Stem(projectReference, claim.ClaimNumber);
        var status = (ValuationClaimStatus)claim.Status;
        var lockedOn = claim.LockedAt ?? claim.PreapprovedAt;
        return new LinkableRecord(
            Type:         RecordType.ValuationClaim,
            RecordId:     claim.ValuationClaimId,
            ProjectId:    claim.ProjectId,
            Reference:    reference,
            TagReference: reference,
            // Led by the period's own name, the name a triager knows the claim by — the VAL stem
            // is a mail tag nobody recognises (decision 2026-08-20).
            Title:        DisplayNameFor(claim),
            StatusLabel:  status.DisplayName(),
            Summary:      status == ValuationClaimStatus.Draft || lockedOn is null
                ? $"Claim {claim.ClaimNumber} — dated {claim.ClaimDate:dd MMM yyyy} — working copy"
                : $"Claim {claim.ClaimNumber} — statement locked {lockedOn:dd MMM yyyy} — payment due £{claim.PaymentDueExVat:N2}",
            // A confirmed period is finished work: still linkable (a late reply belongs to the
            // period it answers), but the live period is what a picker leads with.
            IsActive:     status != ValuationClaimStatus.Confirmed);
    }

    // "August 2026" when named, otherwise "Claim 20" — the one rule ValuationClaim.DisplayName keeps.
    private static string DisplayNameFor(ValuationClaimEntity claim) =>
        string.IsNullOrWhiteSpace(claim.Name) ? $"Claim {claim.ClaimNumber}" : claim.Name.Trim();
}
