using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

// Deletes the event row. Any "JPMS/CAL-####" mailbox tags left behind are harmless — they simply
// no longer resolve to a record — and can be removed from the triage Tagged view like any other
// tag (the same stance as deleting a to-do).
public sealed class DeleteCalendarEventHandler : ICommandHandler<DeleteCalendarEvent, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteCalendarEventHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteCalendarEvent command, CancellationToken cancellationToken)
    {
        var entity = await context.CalendarEvents.FindAsync(new object[] { command.CalendarEventId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Calendar event {command.CalendarEventId} not found.");
        context.CalendarEvents.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.CalendarEventId);
    }
}
