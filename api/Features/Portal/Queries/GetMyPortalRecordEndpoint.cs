using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Portal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Portal.Queries;

/// <summary>
/// GET /api/portal/my/record — the signed-in subcontractor's own record. The subcontractor id
/// comes from the session via SubcontractorScope, never from the client, so a portal login can
/// only ever see its own company. 403 for unlinked or non-subcontractor sessions.
/// </summary>
public sealed class GetMyPortalRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetMyPortalRecord, SubcontractorPortalRecord?> handler;

    public GetMyPortalRecordEndpoint(
        SignedInUserResolver users, IQueryHandler<GetMyPortalRecord, SubcontractorPortalRecord?> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("GetMyPortalRecord")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portal/my/record")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new StatusCodeResult(403);

        // Null body (not 404) when the linked record has been deleted, matching GetClientById —
        // the client store then shows its "no record linked" state rather than treating it as an error.
        var record = await handler.HandleAsync(new GetMyPortalRecord(subcontractorId), cancellationToken);
        return new OkObjectResult(record);
    }
}
