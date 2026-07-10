using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class DeleteValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteValuationInvoiceAuthorisation authorisation;
    private readonly ICommandHandler<DeleteValuationInvoice, Acknowledgement> handler;

    public DeleteValuationInvoiceEndpoint(
        SignedInUserResolver users,
        DeleteValuationInvoiceAuthorisation authorisation,
        ICommandHandler<DeleteValuationInvoice, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "valuation-invoices/{valuationInvoiceId}")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteValuationInvoice(valuationInvoiceId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
