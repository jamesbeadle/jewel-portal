using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class InviteSubcontractorsToBidPackageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly InviteSubcontractorsToBidPackageAuthorisation authorisation;
    private readonly InviteSubcontractorsToBidPackageValidation validation;
    private readonly ICommandHandler<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>> handler;

    public InviteSubcontractorsToBidPackageEndpoint(SignedInUserResolver users, InviteSubcontractorsToBidPackageAuthorisation authorisation, InviteSubcontractorsToBidPackageValidation validation, ICommandHandler<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(InviteSubcontractorsToBidPackage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/recipients")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<InviteSubcontractorsToBidPackage>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
