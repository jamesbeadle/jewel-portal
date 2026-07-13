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

    // The rate library is an internal estimating read; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadRates = JpmsRoleSets.AllInternal;

    [Function(nameof(ListRatesInLibrary))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rates")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRates.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var rates = await handler.HandleAsync(new ListRatesInLibrary(), request.HttpContext.RequestAborted);
        return new OkObjectResult(rates);
    }
}
