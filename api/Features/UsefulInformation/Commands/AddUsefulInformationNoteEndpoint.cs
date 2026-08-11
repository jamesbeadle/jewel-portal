using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class AddUsefulInformationNoteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddUsefulInformationNoteAuthorisation authorisation;
    private readonly AddUsefulInformationNoteValidation validation;
    private readonly ICommandHandler<AddUsefulInformationNote, UsefulInformationNote> handler;

    public AddUsefulInformationNoteEndpoint(SignedInUserResolver users, AddUsefulInformationNoteAuthorisation authorisation, AddUsefulInformationNoteValidation validation, ICommandHandler<AddUsefulInformationNote, UsefulInformationNote> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AddUsefulInformationNote))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/useful-information")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<AddUsefulInformationNote>();
        if (posted is null) return new BadRequestResult();
        if (posted.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        // The author is always the signed-in user — never trusted from the client body.
        var command = posted with { CreatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
