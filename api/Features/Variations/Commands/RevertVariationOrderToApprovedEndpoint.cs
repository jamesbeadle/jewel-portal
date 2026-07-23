using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/revert-to-approved — un-issue a VO back to Approved.</summary>
public sealed class RevertVariationOrderToApprovedEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RevertVariationOrderToApprovedAuthorisation authorisation;
    private readonly RevertVariationOrderToApprovedValidation validation;
    private readonly ICommandHandler<RevertVariationOrderToApproved, VariationOrder> handler;

    public RevertVariationOrderToApprovedEndpoint(
        SignedInUserResolver users,
        RevertVariationOrderToApprovedAuthorisation authorisation,
        RevertVariationOrderToApprovedValidation validation,
        ICommandHandler<RevertVariationOrderToApproved, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RevertVariationOrderToApproved))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/revert-to-approved")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RevertVariationOrderToApproved(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
