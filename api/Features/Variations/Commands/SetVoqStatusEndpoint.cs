using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/voqs/{voqId}/status — move a VOQ between the side-effect-free stages. Body: { status }.
/// Approval/un-approval are refused here (they carry commercial writes and have their own routes).
/// </summary>
public sealed class SetVoqStatusEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetVoqStatusAuthorisation authorisation;
    private readonly SetVoqStatusValidation validation;
    private readonly ICommandHandler<SetVoqStatus, VariationOrderQuote> handler;

    public SetVoqStatusEndpoint(
        SignedInUserResolver users,
        SetVoqStatusAuthorisation authorisation,
        SetVoqStatusValidation validation,
        ICommandHandler<SetVoqStatus, VariationOrderQuote> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetVoqStatus))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "voqs/{voqId}/status")] HttpRequest request,
        string voqId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SetVoqStatus>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderQuoteId = voqId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
