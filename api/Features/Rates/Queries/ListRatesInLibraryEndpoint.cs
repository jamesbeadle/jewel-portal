using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Rates.Queries;

public sealed class ListRatesInLibraryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRatesInLibrary, IReadOnlyList<Rate>> handler;

    public ListRatesInLibraryEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListRatesInLibrary, IReadOnlyList<Rate>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListRatesInLibrary))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rates")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var rates = await handler.HandleAsync(new ListRatesInLibrary(), request.HttpContext.RequestAborted);
        return new OkObjectResult(rates);
    }
}
