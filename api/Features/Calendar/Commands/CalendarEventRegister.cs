using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

/// <summary>
/// Writes the event row itself — numbered on the global CAL sequence, stamped with who raised it.
/// Shared by the Calendar tab's create and the triage create-from-message so both mint the same
/// way (the TenderEnquiryRegister pattern).
/// </summary>
public sealed class CalendarEventRegister
{
    private readonly JpmsContext context;

    public CalendarEventRegister(JpmsContext context) { this.context = context; }

    public async Task<CalendarEventEntity> RaiseAsync(
        string projectId, CalendarEventDetails details, string createdByEmail, CancellationToken cancellationToken)
    {
        var entity = new CalendarEventEntity
        {
            CalendarEventId = CalendarIdentifierFactory.Next(),
            ProjectId = projectId,
            Number = await NextNumberAsync(cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByEmail = createdByEmail
        };
        CalendarEventDetailsRules.Apply(entity, details);
        context.CalendarEvents.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task<int> NextNumberAsync(CancellationToken cancellationToken) =>
        (await context.CalendarEvents.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
}
