using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>
/// An audit row the forms write in the same save as the act it records — a destruction, a look at an
/// emergency form's health answers, a folder's leaving date — so the act and its record can never
/// disagree, as the best-effort AuditTrail may. The detail names the form or the rule, never the person.
/// </summary>
internal static class FormAuditRecords
{
    private const int LongestDetail = 1024;

    public static AuditEventEntity Of(AuditEventType eventType, string recordId, string actorEmail, string detail) => new()
    {
        AuditEventId = FormIdentifierFactory.NextId(),
        OccurredAt = DateTimeOffset.UtcNow,
        ActorEmail = actorEmail,
        EventType = (int)eventType,
        RecordId = recordId,
        Detail = detail.Length > LongestDetail ? detail[..LongestDetail] : detail
    };
}
