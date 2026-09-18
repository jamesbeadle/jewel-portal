using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

/// <summary>
/// POST /api/projects/{projectId}/programme/emails/reply-draft — reply, in the original
/// conversation thread, to a programme-tagged email. JSON body: the
/// <see cref="SendProgrammeReply"/> command; the route's projectId wins over the body's.
/// SaveAsDraftOnly leaves the reviewed draft in the projects mailbox's Drafts folder for Outlook
/// instead of sending; a refused send degrades to exactly that.
/// </summary>
public sealed class SendProgrammeReplyEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendProgrammeReplyAuthorisation authorisation;
    private readonly SendProgrammeReplyValidation validation;
    private readonly ICommandHandler<SendProgrammeReply, ProgrammeReplyOutcome> handler;
    private readonly Audit.AuditActor auditActor;

    public SendProgrammeReplyEndpoint(
        SignedInUserResolver users,
        SendProgrammeReplyAuthorisation authorisation,
        SendProgrammeReplyValidation validation,
        ICommandHandler<SendProgrammeReply, ProgrammeReplyOutcome> handler,
        Audit.AuditActor auditActor)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    [Function(nameof(SendProgrammeReply))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/programme/emails/reply-draft")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // Attribute the dispatcher's audit row to whoever pressed the button.
        auditActor.Email = signedInUser.Email;

        SendProgrammeReply? body = null;
        try { body = await request.ReadFromJsonAsync<SendProgrammeReply>(); }
        catch { /* the validation below reports what's missing */ }
        if (body is null) return new BadRequestResult();

        var command = body with { ProjectId = projectId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // A vanished original email / unconfigured mailbox are user-fixable — surface verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
