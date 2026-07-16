using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/revise-value — revise the value of a live VO. Body: { value }.
/// The reviser is the signed-in user.
/// </summary>
public sealed class ReviseVariationOrderValueEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseVariationOrderValueAuthorisation authorisation;
    private readonly ReviseVariationOrderValueValidation validation;
    private readonly ICommandHandler<ReviseVariationOrderValue, VariationOrder> handler;

    public ReviseVariationOrderValueEndpoint(
        SignedInUserResolver users,
        ReviseVariationOrderValueAuthorisation authorisation,
        ReviseVariationOrderValueValidation validation,
        ICommandHandler<ReviseVariationOrderValue, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReviseVariationOrderValue))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/revise-value")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<ReviseVariationOrderValue>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId, RevisedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
