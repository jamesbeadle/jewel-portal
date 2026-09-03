using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Kpi;

internal static class KpiEntityMapping
{
    public static KpiEmail ToModel(this KpiEmailEntity entity, KpiPersonEntity? person) => new(
        entity.KpiEmailId,
        entity.PersonId,
        person?.Name ?? "(person removed)",
        person?.Email,
        entity.MessageId,
        entity.InternetMessageId,
        entity.ConversationId,
        entity.Subject,
        entity.FromEmail,
        entity.FromName,
        entity.ReceivedAt,
        entity.Note,
        entity.MarkedByEmail,
        entity.MarkedAt,
        entity.Reference);

    public static KpiPerson ToModel(this KpiPersonEntity entity, int kpiCount = 0) =>
        new(entity.KpiPersonId, entity.Name, entity.Email, kpiCount);
}
