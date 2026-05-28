using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateBidPackageScopeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateBidPackageScopeAuthorisation authorisation;
    private readonly UpdateBidPackageScopeValidation validation;
    private readonly ICommandHandler<UpdateBidPackageScope, BidPackage> handler;

    public UpdateBidPackageScopeEndpoint(SignedInUserResolver users, UpdateBidPackageScopeAuthorisation authorisation, UpdateBidPackageScopeValidation validation, ICommandHandler<UpdateBidPackageScope, BidPackage> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(UpdateBidPackageScope))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "bid-packages/{bidPackageId}")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateBidPackageScope>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
