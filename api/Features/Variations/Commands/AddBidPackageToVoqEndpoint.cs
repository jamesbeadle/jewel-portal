using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/voqs/{voqId}/bid-packages — create a bid package under the VOQ. Body: { title, trade }.
/// The owner is the signed-in user.
/// </summary>
public sealed class AddBidPackageToVoqEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddBidPackageToVoqAuthorisation authorisation;
    private readonly AddBidPackageToVoqValidation validation;
    private readonly ICommandHandler<AddBidPackageToVoq, BidPackage> handler;

    public AddBidPackageToVoqEndpoint(
        SignedInUserResolver users,
        AddBidPackageToVoqAuthorisation authorisation,
        AddBidPackageToVoqValidation validation,
        ICommandHandler<AddBidPackageToVoq, BidPackage> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(AddBidPackageToVoq))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "voqs/{voqId}/bid-packages")] HttpRequest request,
        string voqId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<AddBidPackageToVoq>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voqId, OwnerEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
