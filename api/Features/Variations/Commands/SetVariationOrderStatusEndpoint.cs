using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/status — move a variation order between the side-effect-free
/// stages (Quoting, Issued). Body: { status }. Approve / reject / un-approve are refused here (they
/// carry commercial writes and have their own routes).
/// </summary>
public sealed class SetVariationOrderStatusEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetVariationOrderStatusAuthorisation authorisation;
    private readonly SetVariationOrderStatusValidation validation;
    private readonly ICommandHandler<SetVariationOrderStatus, VariationOrder> handler;

    public SetVariationOrderStatusEndpoint(
        SignedInUserResolver users,
        SetVariationOrderStatusAuthorisation authorisation,
        SetVariationOrderStatusValidation validation,
        ICommandHandler<SetVariationOrderStatus, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetVariationOrderStatus))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/status")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SetVariationOrderStatus>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
