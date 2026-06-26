using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class UpdateValuationLineItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly UpdateValuationLineItemValidation validation;
    private readonly ICommandHandler<UpdateValuationLineItem, ValuationLineItem> handler;
    public UpdateValuationLineItemEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, UpdateValuationLineItemValidation validation, ICommandHandler<UpdateValuationLineItem, ValuationLineItem> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateValuationLineItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "valuation-lines/{lineItemId}")] HttpRequest request, string lineItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateValuationLineItem>();
        if (command is null) return new BadRequestResult();
        if (command.ValuationLineItemId != lineItemId) return new BadRequestObjectResult("Route lineItemId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
