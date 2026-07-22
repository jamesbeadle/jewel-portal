using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class GetValuationReportSnapshotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail> handler;
    public GetValuationReportSnapshotEndpoint(SignedInUserResolver users, IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail> handler)
    { this.users = users; this.handler = handler; }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(GetValuationReportSnapshot))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-report-snapshots/{snapshotId}")] HttpRequest request, string snapshotId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new GetValuationReportSnapshot(snapshotId), request.HttpContext.RequestAborted));
    }
}
