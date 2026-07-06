using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendBidPackageInviteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendBidPackageInviteAuthorisation authorisation;
    private readonly SendBidPackageInviteValidation validation;
    private readonly ICommandHandler<SendBidPackageInvite, BidPackage> handler;

    public SendBidPackageInviteEndpoint(SignedInUserResolver users, SendBidPackageInviteAuthorisation authorisation, SendBidPackageInviteValidation validation, ICommandHandler<SendBidPackageInvite, BidPackage> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SendBidPackageInvite))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/send-invite")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SendBidPackageInvite>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Send failures (no recipients, missing Mail.Send) come back as a readable message.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
