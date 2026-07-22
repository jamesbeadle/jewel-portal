using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class DeclineBidPackageRecipientEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeclineBidPackageRecipientAuthorisation authorisation;
    private readonly DeclineBidPackageRecipientValidation validation;
    private readonly ICommandHandler<DeclineBidPackageRecipient, IReadOnlyList<BidPackageRecipient>> handler;

    public DeclineBidPackageRecipientEndpoint(SignedInUserResolver users, DeclineBidPackageRecipientAuthorisation authorisation, DeclineBidPackageRecipientValidation validation, ICommandHandler<DeclineBidPackageRecipient, IReadOnlyList<BidPackageRecipient>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(DeclineBidPackageRecipient))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/recipients/{recipientId}/decline")] HttpRequest request,
        string bidPackageId, string recipientId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<DeclineBidPackageRecipient>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");
        if (command.RecipientId != recipientId) return new BadRequestObjectResult("Route recipientId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
