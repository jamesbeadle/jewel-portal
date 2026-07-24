using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// DELETE /api/variation-orders/{voId} — delete a non-approved variation order (and cascade its
/// bid-package tender data). Guards live in the handler.
/// </summary>
public sealed class DeleteVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteVariationOrderAuthorisation authorisation;
    private readonly DeleteVariationOrderValidation validation;
    private readonly ICommandHandler<DeleteVariationOrder, Acknowledgement> handler;

    public DeleteVariationOrderEndpoint(
        SignedInUserResolver users,
        DeleteVariationOrderAuthorisation authorisation,
        DeleteVariationOrderValidation validation,
        ICommandHandler<DeleteVariationOrder, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(DeleteVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "variation-orders/{voId}")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteVariationOrder(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
