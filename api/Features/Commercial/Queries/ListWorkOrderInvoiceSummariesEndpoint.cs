using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListWorkOrderInvoiceSummariesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListWorkOrderInvoiceSummaries, IReadOnlyList<WorkOrderInvoiceSummary>> handler;

    public ListWorkOrderInvoiceSummariesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListWorkOrderInvoiceSummaries, IReadOnlyList<WorkOrderInvoiceSummary>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListWorkOrderInvoiceSummaries))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/work-order-invoice-summaries")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var summaries = await handler.HandleAsync(new ListWorkOrderInvoiceSummaries(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(summaries);
    }
}
