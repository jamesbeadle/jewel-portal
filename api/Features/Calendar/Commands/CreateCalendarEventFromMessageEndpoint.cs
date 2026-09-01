using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class CreateCalendarEventFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly CreateCalendarEventFromMessageAuthorisation authorisation;
    private readonly CreateCalendarEventFromMessageValidation validation;
    private readonly ICommandHandler<CreateCalendarEventFromMessage, CalendarEvent> handler;

    public CreateCalendarEventFromMessageEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        CreateCalendarEventFromMessageAuthorisation authorisation, CreateCalendarEventFromMessageValidation validation,
        ICommandHandler<CreateCalendarEventFromMessage, CalendarEvent> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateCalendarEventFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-calendar-event")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateCalendarEventFromMessage>(cancellationToken);
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId))
            return new BadRequestObjectResult("messageId is required.");

        // The creator is always the signed-in user — never trusted from the client body.
        var command = posted with { CreatedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // Guards (cross-pathway confirm, missing project, unreadable email) are answers the
            // triager acts on — a bodiless 500 would hide them.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
