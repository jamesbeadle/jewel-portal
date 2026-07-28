using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RemoveCompanyContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpsertCompanyContactAuthorisation authorisation;
    private readonly ICommandHandler<RemoveCompanyContact, Acknowledgement> handler;

    public RemoveCompanyContactEndpoint(SignedInUserResolver users, UpsertCompanyContactAuthorisation authorisation, ICommandHandler<RemoveCompanyContact, Acknowledgement> handler)
    {
        this.users = users; this.authorisation = authorisation; this.handler = handler;
    }

    [Function(nameof(RemoveCompanyContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "subcontractors/{subcontractorId}/contacts/{companyContactId}")] HttpRequest request,
        string subcontractorId, string companyContactId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveCompanyContact(subcontractorId, companyContactId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (contact gone) are answers to what was asked, not faults — 400 so the
            // dialog shows the reason instead of the client falling back to an opaque 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
