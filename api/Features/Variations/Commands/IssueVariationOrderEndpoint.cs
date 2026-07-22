using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/issue — mark an approved VO as issued.</summary>
public sealed class IssueVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IssueVariationOrderAuthorisation authorisation;
    private readonly IssueVariationOrderValidation validation;
    private readonly ICommandHandler<IssueVariationOrder, VariationOrder> handler;

    public IssueVariationOrderEndpoint(
        SignedInUserResolver users,
        IssueVariationOrderAuthorisation authorisation,
        IssueVariationOrderValidation validation,
        ICommandHandler<IssueVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(IssueVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/issue")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new IssueVariationOrder(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
