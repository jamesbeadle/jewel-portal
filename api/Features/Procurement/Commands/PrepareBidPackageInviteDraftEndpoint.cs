using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class PrepareBidPackageInviteDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareBidPackageInviteDraftAuthorisation authorisation;
    private readonly PrepareBidPackageInviteDraftValidation validation;
    private readonly ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft> handler;

    public PrepareBidPackageInviteDraftEndpoint(SignedInUserResolver users, PrepareBidPackageInviteDraftAuthorisation authorisation, PrepareBidPackageInviteDraftValidation validation, ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(PrepareBidPackageInviteDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/draft-invite")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<PrepareBidPackageInviteDraft>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Draft failures (no recipients, mailbox connection) come back as a readable message.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
