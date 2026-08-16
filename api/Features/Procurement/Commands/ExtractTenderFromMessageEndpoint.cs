using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ExtractTenderFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ExtractTenderFromMessageAuthorisation authorisation;
    private readonly ExtractTenderFromMessageValidation validation;
    private readonly ICommandHandler<ExtractTenderFromMessage, TenderExtraction> handler;

    public ExtractTenderFromMessageEndpoint(
        SignedInUserResolver users, ExtractTenderFromMessageAuthorisation authorisation,
        ExtractTenderFromMessageValidation validation, ICommandHandler<ExtractTenderFromMessage, TenderExtraction> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(ExtractTenderFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/extract-tender")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ExtractTenderFromMessage>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
