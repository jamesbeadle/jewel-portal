using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListPrelimEntriesForItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPrelimEntriesForItem, IReadOnlyList<PrelimForecastEntry>> handler;
    public ListPrelimEntriesForItemEndpoint(SignedInUserResolver users, IQueryHandler<ListPrelimEntriesForItem, IReadOnlyList<PrelimForecastEntry>> handler) { this.users = users; this.handler = handler; }

    // CVR reads are internal-only; external portal logins have no view of margin and forecast.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListPrelimEntriesForItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "prelims/{prelimItemId}/entries")] HttpRequest request, string prelimItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListPrelimEntriesForItem(prelimItemId), request.HttpContext.RequestAborted));
    }
}
