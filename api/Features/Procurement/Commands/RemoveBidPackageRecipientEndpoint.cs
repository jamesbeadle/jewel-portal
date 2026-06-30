using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class RemoveBidPackageRecipientEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveBidPackageRecipientAuthorisation authorisation;
    private readonly RemoveBidPackageRecipientValidation validation;
    private readonly ICommandHandler<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>> handler;

    public RemoveBidPackageRecipientEndpoint(SignedInUserResolver users, RemoveBidPackageRecipientAuthorisation authorisation, RemoveBidPackageRecipientValidation validation, ICommandHandler<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(RemoveBidPackageRecipient))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "bid-packages/{bidPackageId}/recipients/{recipientId}")] HttpRequest request,
        string bidPackageId, string recipientId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveBidPackageRecipient(bidPackageId, recipientId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
