using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class ListXeroTrackingCategoriesEndpoint
{
    // Mirrors the Cost codes page's gate (Admin expands to every role at resolution): the
    // people who manage the cost-code master are the ones who need to see Xero's side of it.
    private static readonly RoleSet AllowedToListTrackingCategories = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroTrackingCategories, XeroTrackingCategoriesSnapshot> handler;

    public ListXeroTrackingCategoriesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListXeroTrackingCategories, XeroTrackingCategoriesSnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListXeroTrackingCategories))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/tracking-categories")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToListTrackingCategories.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new ListXeroTrackingCategories(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
