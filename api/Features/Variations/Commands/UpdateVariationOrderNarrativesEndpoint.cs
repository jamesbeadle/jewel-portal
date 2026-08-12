using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/narratives — re-state the official document's narrative
/// sections. Body: { commercialBasis, programmeImpact, exclusions }. Allowed at every stage;
/// wording only (see UpdateVariationOrderNarratives).
/// </summary>
public sealed class UpdateVariationOrderNarrativesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateVariationOrderNarrativesAuthorisation authorisation;
    private readonly UpdateVariationOrderNarrativesValidation validation;
    private readonly ICommandHandler<UpdateVariationOrderNarratives, VariationOrder> handler;

    public UpdateVariationOrderNarrativesEndpoint(
        SignedInUserResolver users,
        UpdateVariationOrderNarrativesAuthorisation authorisation,
        UpdateVariationOrderNarrativesValidation validation,
        ICommandHandler<UpdateVariationOrderNarratives, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateVariationOrderNarratives))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/narratives")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<UpdateVariationOrderNarratives>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
