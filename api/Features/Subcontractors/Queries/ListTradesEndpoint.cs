using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListTradesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTrades, IReadOnlyList<Trade>> handler;

    public ListTradesEndpoint(SignedInUserResolver users, IQueryHandler<ListTrades, IReadOnlyList<Trade>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListTrades))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "trades")] HttpRequest request)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTrades(), request.HttpContext.RequestAborted));
    }
}
