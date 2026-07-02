using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// POST /api/projects/{projectId}/valuation-invoices — raise a monthly valuation invoice. Body: { periodMonth,
/// amount, valuationClaimId? }.
/// </summary>
public sealed class CreateValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateValuationInvoiceAuthorisation authorisation;
    private readonly CreateValuationInvoiceValidation validation;
    private readonly ICommandHandler<CreateValuationInvoice, ValuationInvoice> handler;

    public CreateValuationInvoiceEndpoint(
        SignedInUserResolver users,
        CreateValuationInvoiceAuthorisation authorisation,
        CreateValuationInvoiceValidation validation,
        ICommandHandler<CreateValuationInvoice, ValuationInvoice> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/valuation-invoices")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<CreateValuationInvoice>();
        if (body is null) return new BadRequestResult();

        var command = body with { ProjectId = projectId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
