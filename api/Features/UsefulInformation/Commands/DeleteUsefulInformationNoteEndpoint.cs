using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.UsefulInformation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class DeleteUsefulInformationNoteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteUsefulInformationNoteAuthorisation authorisation;
    private readonly DeleteUsefulInformationNoteValidation validation;
    private readonly ICommandHandler<DeleteUsefulInformationNote, Acknowledgement> handler;

    public DeleteUsefulInformationNoteEndpoint(SignedInUserResolver users, DeleteUsefulInformationNoteAuthorisation authorisation, DeleteUsefulInformationNoteValidation validation, ICommandHandler<DeleteUsefulInformationNote, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DeleteUsefulInformationNote))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "useful-information-notes/{noteId}")] HttpRequest request, string noteId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteUsefulInformationNote(noteId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
