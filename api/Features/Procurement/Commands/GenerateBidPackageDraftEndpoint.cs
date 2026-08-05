using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class GenerateBidPackageDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly GenerateBidPackageDraftAuthorisation authorisation;
    private readonly GenerateBidPackageDraftValidation validation;
    private readonly ICommandHandler<GenerateBidPackageDraft, BidPackageDraftProposal> handler;

    public GenerateBidPackageDraftEndpoint(SignedInUserResolver users, GenerateBidPackageDraftAuthorisation authorisation, GenerateBidPackageDraftValidation validation, ICommandHandler<GenerateBidPackageDraft, BidPackageDraftProposal> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(GenerateBidPackageDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/generate-draft")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<GenerateBidPackageDraft>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
