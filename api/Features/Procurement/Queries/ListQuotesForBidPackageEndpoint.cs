using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListQuotesForBidPackageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>> handler;

    public ListQuotesForBidPackageEndpoint(SignedInUserResolver users, IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListQuotesForBidPackage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/quotes")] HttpRequest request,
        string bidPackageId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListQuotesForBidPackage(bidPackageId), request.HttpContext.RequestAborted));
    }
}
