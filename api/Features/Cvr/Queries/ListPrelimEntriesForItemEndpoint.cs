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

    [Function(nameof(ListPrelimEntriesForItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "prelims/{prelimItemId}/entries")] HttpRequest request, string prelimItemId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListPrelimEntriesForItem(prelimItemId), request.HttpContext.RequestAborted));
    }
}
