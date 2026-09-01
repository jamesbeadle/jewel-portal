using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class CreateCalendarEventEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateCalendarEventAuthorisation authorisation;
    private readonly CreateCalendarEventValidation validation;
    private readonly ICommandHandler<CreateCalendarEvent, CalendarEvent> handler;

    public CreateCalendarEventEndpoint(
        SignedInUserResolver users, CreateCalendarEventAuthorisation authorisation,
        CreateCalendarEventValidation validation, ICommandHandler<CreateCalendarEvent, CalendarEvent> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateCalendarEvent))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/calendar-events")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateCalendarEvent>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A calendar event body is required.");

        // The creator is always the signed-in user — never trusted from the client body — and the
        // project is the route's, whatever the body claimed.
        var command = posted with { ProjectId = projectId, CreatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
