using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/title — retitle a variation order. Body: { title }. Allowed at
/// every stage; nothing already written downstream is rewritten (see RenameVariationOrder).
/// </summary>
public sealed class RenameVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RenameVariationOrderAuthorisation authorisation;
    private readonly RenameVariationOrderValidation validation;
    private readonly ICommandHandler<RenameVariationOrder, VariationOrder> handler;

    public RenameVariationOrderEndpoint(
        SignedInUserResolver users,
        RenameVariationOrderAuthorisation authorisation,
        RenameVariationOrderValidation validation,
        ICommandHandler<RenameVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RenameVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/title")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<RenameVariationOrder>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
