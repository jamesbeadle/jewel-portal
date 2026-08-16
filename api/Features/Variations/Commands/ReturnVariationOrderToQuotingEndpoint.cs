using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/return-to-quoting — un-approve back to Quoting
/// (repairs records approved in error).</summary>
public sealed class ReturnVariationOrderToQuotingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReturnVariationOrderToQuotingAuthorisation authorisation;
    private readonly ReturnVariationOrderToQuotingValidation validation;
    private readonly ICommandHandler<ReturnVariationOrderToQuoting, VariationOrder> handler;

    public ReturnVariationOrderToQuotingEndpoint(
        SignedInUserResolver users,
        ReturnVariationOrderToQuotingAuthorisation authorisation,
        ReturnVariationOrderToQuotingValidation validation,
        ICommandHandler<ReturnVariationOrderToQuoting, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReturnVariationOrderToQuoting))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/return-to-quoting")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // Consume the command envelope the client posts with every command (JPMS-C89747).
        // The order id in the route is authoritative — the body is drained, not trusted —
        // but it must be READ: every working sibling endpoint reads its body, and this
        // endpoint (which didn't) was the one whose requests stalled to the platform's
        // 45-second kill. Never leave a request body unconsumed behind the SWA proxy.
        try { _ = await request.ReadFromJsonAsync<ReturnVariationOrderToQuoting>(cancellationToken); }
        catch (System.Text.Json.JsonException) { /* absent or malformed body — the route already names the order */ }

        var command = new ReturnVariationOrderToQuoting(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
