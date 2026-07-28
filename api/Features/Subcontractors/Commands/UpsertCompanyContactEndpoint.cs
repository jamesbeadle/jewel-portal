using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpsertCompanyContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpsertCompanyContactAuthorisation authorisation;
    private readonly UpsertCompanyContactValidation validation;
    private readonly ICommandHandler<UpsertCompanyContact, CompanyContact> handler;

    public UpsertCompanyContactEndpoint(SignedInUserResolver users, UpsertCompanyContactAuthorisation authorisation, UpsertCompanyContactValidation validation, ICommandHandler<UpsertCompanyContact, CompanyContact> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(UpsertCompanyContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/contacts")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpsertCompanyContact>();
        if (command is null || !string.Equals(command.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
            return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (record gone, contact gone) are answers to what was asked, not faults — 400 so the
            // dialog shows the reason instead of the client falling back to an opaque 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
