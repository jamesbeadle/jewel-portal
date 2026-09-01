using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Calendar;

internal static class CalendarEntityMapping
{
    public static CalendarEvent ToModel(this CalendarEventEntity entity) => new(
        entity.CalendarEventId,
        entity.ProjectId,
        entity.Number,
        entity.Reference,
        entity.Title,
        (CalendarEventKind)entity.Kind,
        entity.Date,
        entity.StartTime,
        entity.EndDate,
        entity.Notes,
        entity.ClientVisible,
        entity.CreatedByEmail,
        entity.CreatedAt);
}
