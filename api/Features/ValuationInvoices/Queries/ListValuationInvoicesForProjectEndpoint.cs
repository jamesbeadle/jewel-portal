using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class ListValuationInvoicesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>> handler;

    public ListValuationInvoicesForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Valuation invoice reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListValuationInvoicesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/valuation-invoices")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var calls = await handler.HandleAsync(new ListValuationInvoicesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(calls);
    }
}
