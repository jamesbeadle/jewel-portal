using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AddBidPackageLineItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddBidPackageLineItemsAuthorisation authorisation;
    private readonly AddBidPackageLineItemsValidation validation;
    private readonly ICommandHandler<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>> handler;

    public AddBidPackageLineItemsEndpoint(SignedInUserResolver users, AddBidPackageLineItemsAuthorisation authorisation, AddBidPackageLineItemsValidation validation, ICommandHandler<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(AddBidPackageLineItems))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/line-items")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<AddBidPackageLineItems>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
