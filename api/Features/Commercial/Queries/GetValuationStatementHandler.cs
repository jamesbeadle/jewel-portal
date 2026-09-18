using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// A valuation as its statement — the locked claim's own frozen rows, or a Draft's working copy
/// (ValuationStatementLines.ReadAsync). The id may be a claim id or a RETIRED snapshot id: until
/// 2026-09-18 the statement was a separate object, and an old link, a saved invoice row or a
/// connector call that still names one lands on the claim it was frozen from through the alias
/// register. Not found either way is an InvalidOperationException the endpoints turn into 404.
/// </summary>
public sealed class GetValuationStatementHandler : IQueryHandler<GetValuationStatement, ValuationStatement>
{
    private readonly JpmsContext context;
    public GetValuationStatementHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationStatement> HandleAsync(GetValuationStatement query, CancellationToken cancellationToken)
    {
        var claim = await ResolveClaimAsync(context, query.ValuationClaimId, cancellationToken)
            ?? throw new InvalidOperationException($"Valuation {query.ValuationClaimId} not found.");
        return await ValuationStatementLines.ReadAsync(context, claim, cancellationToken);
    }

    /// <summary>The claim an id names — directly, or through a retired snapshot's alias row.</summary>
    internal static async Task<ValuationClaimEntity?> ResolveClaimAsync(JpmsContext context, string id, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.ValuationClaimId == id, cancellationToken);
        if (claim is not null) return claim;

        var aliasedClaimId = await context.ValuationClaimLegacyStatements.AsNoTracking()
            .Where(alias => alias.ValuationReportSnapshotId == id)
            .Select(alias => alias.ValuationClaimId)
            .FirstOrDefaultAsync(cancellationToken);
        if (aliasedClaimId is null) return null;
        return await context.ValuationClaims.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.ValuationClaimId == aliasedClaimId, cancellationToken);
    }
}
