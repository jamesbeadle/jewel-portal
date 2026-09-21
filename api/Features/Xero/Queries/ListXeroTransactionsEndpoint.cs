using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class ListXeroTransactionsEndpoint
{
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
        if (!XeroReportRoles.TransactionReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new ListXeroTransactions(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
