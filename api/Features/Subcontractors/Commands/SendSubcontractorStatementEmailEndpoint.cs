using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// POST /api/subcontractors/{subcontractorId}/statement/draft-email — email the statement of
/// account to the subcontractor from the shared projects mailbox, the statement rendered from the
/// live register and attached as a PDF. SaveAsDraftOnly leaves the reviewed draft in Drafts for
/// Outlook instead of sending.
///
/// The command, its handler, its gates and its DI registration were all written on 2026-09-17 and
/// this endpoint was not, so the client's route answered 404 and the Directory's "Email statement"
/// button has never worked. Every api endpoint is an explicit Function with its own HttpTrigger —
/// nothing dispatches a command by convention — so a missing endpoint is a missing door.
/// </summary>
public sealed class SendSubcontractorStatementEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendSubcontractorStatementEmailAuthorisation authorisation;
    private readonly SendSubcontractorStatementEmailValidation validation;
    private readonly ICommandHandler<SendSubcontractorStatementEmail, SubcontractorStatementEmailOutcome> handler;
    private readonly AuditActor auditActor;

    public SendSubcontractorStatementEmailEndpoint(
        SignedInUserResolver users,
        SendSubcontractorStatementEmailAuthorisation authorisation,
        SendSubcontractorStatementEmailValidation validation,
        ICommandHandler<SendSubcontractorStatementEmail, SubcontractorStatementEmailOutcome> handler,
        AuditActor auditActor)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    [Function(nameof(SendSubcontractorStatementEmail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "subcontractors/{subcontractorId}/statement/draft-email")] HttpRequest request,
        string subcontractorId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // The dispatcher's audit row attributes the email to whoever pressed the button — commands
        // don't carry the caller's identity, so it reaches the handler through the scoped actor.
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<SendSubcontractorStatementEmail>();
        if (command is null) return new BadRequestResult();
        if (command.SubcontractorId != subcontractorId)
            return new BadRequestObjectResult("Route subcontractorId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // No directory email, or an unconfigured mailbox — user-fixable, so the modal shows it.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
