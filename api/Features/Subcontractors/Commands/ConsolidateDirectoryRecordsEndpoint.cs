using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class ConsolidateDirectoryRecordsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ConsolidateDirectoryRecordsAuthorisation authorisation;
    private readonly ConsolidateDirectoryRecordsValidation validation;
    private readonly ICommandHandler<ConsolidateDirectoryRecords, Subcontractor> handler;

    public ConsolidateDirectoryRecordsEndpoint(SignedInUserResolver users, ConsolidateDirectoryRecordsAuthorisation authorisation, ConsolidateDirectoryRecordsValidation validation, ICommandHandler<ConsolidateDirectoryRecords, Subcontractor> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(ConsolidateDirectoryRecords))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/consolidate")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ConsolidateDirectoryRecords>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (master or a merged record gone, nothing left to merge) are answers to what was asked, not faults — 400 so the
            // dialog shows the reason instead of the client falling back to an opaque 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
