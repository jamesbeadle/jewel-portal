using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageDrawingsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBidPackageDrawings, IReadOnlyList<Drawing>> handler;

    public ListBidPackageDrawingsEndpoint(SignedInUserResolver users, IQueryHandler<ListBidPackageDrawings, IReadOnlyList<Drawing>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function(nameof(ListBidPackageDrawings))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/drawings")] HttpRequest request,
        string bidPackageId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListBidPackageDrawings(bidPackageId), request.HttpContext.RequestAborted));
    }
}
