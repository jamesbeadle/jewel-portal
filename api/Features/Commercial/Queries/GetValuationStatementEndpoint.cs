using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET /api/valuation-claims/{claimId}/statement — the valuation as its statement (frozen rows
/// when locked, working copy when Draft). GET /api/valuation-report-snapshots/{snapshotId} is the
/// retired address of the same thing (2026-09-18), kept so old links open the claim's statement.
/// </summary>
public sealed class GetValuationStatementEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetValuationStatement, ValuationStatement> handler;
    public GetValuationStatementEndpoint(SignedInUserResolver users, IQueryHandler<GetValuationStatement, ValuationStatement> handler)
    { this.users = users; this.handler = handler; }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(GetValuationStatement))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-claims/{claimId}/statement")] HttpRequest request, string claimId)
        => RespondAsync(request, claimId);

    [Function("GetValuationStatementByRetiredSnapshotId")]
    public Task<IActionResult> RunLegacy([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-report-snapshots/{snapshotId}")] HttpRequest request, string snapshotId)
        => RespondAsync(request, snapshotId);

    private async Task<IActionResult> RespondAsync(HttpRequest request, string id)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(new GetValuationStatement(id), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }
    }
}
