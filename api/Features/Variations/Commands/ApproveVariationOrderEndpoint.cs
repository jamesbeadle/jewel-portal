using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/approve — approve the variation order and write the contract
/// figures. Body: { costCode, value? }. The approver is the signed-in user.
/// </summary>
public sealed class ApproveVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveVariationOrderAuthorisation authorisation;
    private readonly ApproveVariationOrderValidation validation;
    private readonly ICommandHandler<ApproveVariationOrder, VariationOrder> handler;

    public ApproveVariationOrderEndpoint(
        SignedInUserResolver users,
        ApproveVariationOrderAuthorisation authorisation,
        ApproveVariationOrderValidation validation,
        ICommandHandler<ApproveVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ApproveVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/approve")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<ApproveVariationOrder>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId, ApprovedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
