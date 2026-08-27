using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

/// <summary>Rewrites the event's editable face. The reference, the creator stamp and the mail
/// tagged to it are untouched — the CAL number never changes, so links keep working.</summary>
public sealed class UpdateCalendarEventHandler : ICommandHandler<UpdateCalendarEvent, CalendarEvent>
{
    private readonly JpmsContext context;

    public UpdateCalendarEventHandler(JpmsContext context) { this.context = context; }

    public async Task<CalendarEvent> HandleAsync(UpdateCalendarEvent command, CancellationToken cancellationToken)
    {
        var entity = await context.CalendarEvents
            .FirstOrDefaultAsync(row => row.CalendarEventId == command.CalendarEventId, cancellationToken)
            ?? throw new InvalidOperationException($"Calendar event '{command.CalendarEventId}' not found.");

        CalendarEventDetailsRules.Apply(entity, command.Details);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
