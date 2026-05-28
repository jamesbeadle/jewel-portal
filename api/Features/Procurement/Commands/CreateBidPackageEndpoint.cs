using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateBidPackageAuthorisation authorisation;
    private readonly CreateBidPackageValidation validation;
    private readonly ICommandHandler<CreateBidPackage, BidPackage> handler;

    public CreateBidPackageEndpoint(SignedInUserResolver users, CreateBidPackageAuthorisation authorisation, CreateBidPackageValidation validation, ICommandHandler<CreateBidPackage, BidPackage> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(CreateBidPackage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/bid-packages")] HttpRequest request,
        string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateBidPackage>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
