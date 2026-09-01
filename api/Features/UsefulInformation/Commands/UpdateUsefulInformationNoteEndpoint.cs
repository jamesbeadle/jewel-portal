using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class UpdateUsefulInformationNoteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateUsefulInformationNoteAuthorisation authorisation;
    private readonly UpdateUsefulInformationNoteValidation validation;
    private readonly ICommandHandler<UpdateUsefulInformationNote, UsefulInformationNote> handler;

    public UpdateUsefulInformationNoteEndpoint(SignedInUserResolver users, UpdateUsefulInformationNoteAuthorisation authorisation, UpdateUsefulInformationNoteValidation validation, ICommandHandler<UpdateUsefulInformationNote, UsefulInformationNote> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateUsefulInformationNote))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "useful-information-notes/{noteId}")] HttpRequest request, string noteId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateUsefulInformationNote>();
        if (posted is null) return new BadRequestResult();
        if (posted.UsefulInformationNoteId != noteId) return new BadRequestObjectResult("Route noteId does not match body.");

        // The editor is always the signed-in user — never trusted from the client body.
        var command = posted with { UpdatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
