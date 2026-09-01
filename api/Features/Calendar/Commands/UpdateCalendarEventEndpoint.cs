using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class UpdateCalendarEventEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateCalendarEventAuthorisation authorisation;
    private readonly UpdateCalendarEventValidation validation;
    private readonly ICommandHandler<UpdateCalendarEvent, CalendarEvent> handler;

    public UpdateCalendarEventEndpoint(
        SignedInUserResolver users, UpdateCalendarEventAuthorisation authorisation,
        UpdateCalendarEventValidation validation, ICommandHandler<UpdateCalendarEvent, CalendarEvent> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateCalendarEvent))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "calendar-events/{calendarEventId}")] HttpRequest request,
        string calendarEventId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateCalendarEvent>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A calendar event body is required.");
        var command = posted with { CalendarEventId = calendarEventId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
