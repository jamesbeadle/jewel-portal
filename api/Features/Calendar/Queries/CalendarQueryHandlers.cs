using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Queries;

public sealed class ListCalendarEventsForProjectHandler
    : IQueryHandler<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>>
{
    private readonly JpmsContext context;
    public ListCalendarEventsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CalendarEvent>> HandleAsync(
        ListCalendarEventsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.CalendarEvents.AsNoTracking()
            .Where(e => e.ProjectId == query.ProjectId)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ThenBy(e => e.Number)
            .ToListAsync(cancellationToken);
        return entities.Select(e => e.ToModel()).ToList().AsReadOnly();
    }
}
