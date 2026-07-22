using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageLineItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetBidPackageLineItemsAuthorisation authorisation;
    private readonly SetBidPackageLineItemsValidation validation;
    private readonly ICommandHandler<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>> handler;

    public SetBidPackageLineItemsEndpoint(SignedInUserResolver users, SetBidPackageLineItemsAuthorisation authorisation, SetBidPackageLineItemsValidation validation, ICommandHandler<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SetBidPackageLineItems))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "bid-packages/{bidPackageId}/line-items")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetBidPackageLineItems>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
