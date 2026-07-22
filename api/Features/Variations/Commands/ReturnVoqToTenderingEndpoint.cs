using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/voqs/{voqId}/return-to-tendering — un-approve a VOQ back to Tendering.</summary>
public sealed class ReturnVoqToTenderingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReturnVoqToTenderingAuthorisation authorisation;
    private readonly ReturnVoqToTenderingValidation validation;
    private readonly ICommandHandler<ReturnVoqToTendering, VariationOrderQuote> handler;

    public ReturnVoqToTenderingEndpoint(
        SignedInUserResolver users,
        ReturnVoqToTenderingAuthorisation authorisation,
        ReturnVoqToTenderingValidation validation,
        ICommandHandler<ReturnVoqToTendering, VariationOrderQuote> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReturnVoqToTendering))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "voqs/{voqId}/return-to-tendering")] HttpRequest request,
        string voqId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new ReturnVoqToTendering(voqId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
