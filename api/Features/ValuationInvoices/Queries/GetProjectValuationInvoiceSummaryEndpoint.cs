using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class GetProjectValuationInvoiceSummaryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary> handler;

    public GetProjectValuationInvoiceSummaryEndpoint(SignedInUserResolver users, IQueryHandler<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProjectValuationInvoiceSummary))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/valuation-invoices/summary")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var summary = await handler.HandleAsync(new GetProjectValuationInvoiceSummary(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(summary);
    }
}
