using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/voqs/{voqId}/select-tender — record the winning tender. Body: { bidPackageId,
/// subcontractorId, estimatedValue? }.
/// </summary>
public sealed class SelectVoqTenderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SelectVoqTenderAuthorisation authorisation;
    private readonly SelectVoqTenderValidation validation;
    private readonly ICommandHandler<SelectVoqTender, VariationOrderQuote> handler;

    public SelectVoqTenderEndpoint(
        SignedInUserResolver users,
        SelectVoqTenderAuthorisation authorisation,
        SelectVoqTenderValidation validation,
        ICommandHandler<SelectVoqTender, VariationOrderQuote> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SelectVoqTender))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "voqs/{voqId}/select-tender")] HttpRequest request,
        string voqId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SelectVoqTender>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderQuoteId = voqId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
