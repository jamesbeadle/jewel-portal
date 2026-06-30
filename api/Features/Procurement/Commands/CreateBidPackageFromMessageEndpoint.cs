using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateBidPackageFromMessageAuthorisation authorisation;
    private readonly CreateBidPackageFromMessageValidation validation;
    private readonly ICommandHandler<CreateBidPackageFromMessage, BidPackage> handler;

    public CreateBidPackageFromMessageEndpoint(SignedInUserResolver users, CreateBidPackageFromMessageAuthorisation authorisation, CreateBidPackageFromMessageValidation validation, ICommandHandler<CreateBidPackageFromMessage, BidPackage> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(CreateBidPackageFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-bid-package")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateBidPackageFromMessage>();
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId) || string.IsNullOrWhiteSpace(posted.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // The owner is always the signed-in user — never trusted from the client body.
        var command = posted with { OwnerEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
