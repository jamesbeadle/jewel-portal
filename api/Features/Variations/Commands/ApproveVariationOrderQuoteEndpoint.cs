using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/voqs/{voqId}/approve — approve the VOQ and raise a Variation Order. Body: { costCode,
/// value? }. The approver is the signed-in user.
/// </summary>
public sealed class ApproveVariationOrderQuoteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveVariationOrderQuoteAuthorisation authorisation;
    private readonly ApproveVariationOrderQuoteValidation validation;
    private readonly ICommandHandler<ApproveVariationOrderQuote, VariationOrder> handler;

    public ApproveVariationOrderQuoteEndpoint(
        SignedInUserResolver users,
        ApproveVariationOrderQuoteAuthorisation authorisation,
        ApproveVariationOrderQuoteValidation validation,
        ICommandHandler<ApproveVariationOrderQuote, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ApproveVariationOrderQuote))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "voqs/{voqId}/approve")] HttpRequest request,
        string voqId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<ApproveVariationOrderQuote>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderQuoteId = voqId, ApprovedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
