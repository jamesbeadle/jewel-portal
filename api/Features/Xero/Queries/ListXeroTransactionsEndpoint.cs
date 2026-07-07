using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class ListXeroTransactionsEndpoint
{
    // Ledger data is financially sensitive — mirror the cost-codes audience (finance-facing roles).
    // Admins pass automatically because Role.Admin is included explicitly here.
    private static readonly RoleSet AllowedToViewLedger = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroTransactions, XeroTransactionsSnapshot> handler;

    public ListXeroTransactionsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListXeroTransactions, XeroTransactionsSnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListXeroTransactions))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/transactions")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToViewLedger.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new ListXeroTransactions(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
