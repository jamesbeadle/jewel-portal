using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Calendar.Queries;

/// <summary>The calendar reads — every event on one project. Internal-only for now: events carry
/// ClientVisible ready for a client surface, but that surface gets its own scoped gate when it
/// is built (CalendarRoles).</summary>
public sealed class CalendarQueryEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>> list;

    public CalendarQueryEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>> list)
    {
        this.users = users;
        this.list = list;
    }

    [Function(nameof(ListCalendarEventsForProject))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/calendar-events")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!CalendarRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await list.HandleAsync(new ListCalendarEventsForProject(projectId), cancellationToken));
    }
}
