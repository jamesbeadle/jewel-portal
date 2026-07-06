using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListQuoteLineItemsForBidPackageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>> handler;

    public ListQuoteLineItemsForBidPackageEndpoint(SignedInUserResolver users, IQueryHandler<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function(nameof(ListQuoteLineItemsForBidPackage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/quote-lines")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var result = await handler.HandleAsync(new ListQuoteLineItemsForBidPackage(bidPackageId), request.HttpContext.RequestAborted);
        return new OkObjectResult(result);
    }
}
