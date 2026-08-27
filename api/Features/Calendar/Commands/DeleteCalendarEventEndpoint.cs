using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class DeleteCalendarEventEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteCalendarEventAuthorisation authorisation;
    private readonly ICommandHandler<DeleteCalendarEvent, Acknowledgement> handler;

    public DeleteCalendarEventEndpoint(
        SignedInUserResolver users, DeleteCalendarEventAuthorisation authorisation,
        ICommandHandler<DeleteCalendarEvent, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteCalendarEvent))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "calendar-events/{calendarEventId}")] HttpRequest request,
        string calendarEventId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteCalendarEvent(calendarEventId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        if (string.IsNullOrWhiteSpace(calendarEventId)) return new BadRequestObjectResult("calendarEventId is required.");

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
