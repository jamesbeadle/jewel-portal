using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Data;

namespace Jewel.JPMS.Api.Features.Hs.Notifications;

/// <summary>
/// The one writer of a record's events: every door that changes a corrective action — a comment,
/// a status move, an audit's Issue minting it — leaves one row here, and the digest sweep tells
/// the other side. Nothing is sent from the door itself, so a burst of changes in one sitting is
/// one email, never one per item (Katy-Louise, 15 Sep 2026).
/// </summary>
public static class HsRecordEvents
{
    public static HsRecordEventEntity Record(
        JpmsContext context, HsRecordEntity record, HsRecordEventKind kind, string detail, string byEmail, string byName, DateTimeOffset now)
    {
        var occurrence = new HsRecordEventEntity
        {
            HsRecordEventId = HsIdentifierFactory.NextHsRecordEventId(),
            HsRecordId = record.HsRecordId,
            ProjectId = record.ProjectId,
            Kind = (int)kind,
            Detail = detail,
            ByEmail = byEmail.Trim(),
            ByName = byName.Trim(),
            OccurredAt = now
        };
        context.HsRecordEvents.Add(occurrence);
        return occurrence;
    }

    public static string StatusChangeDetail(HsStatus from, HsStatus to) => $"{from.DisplayName()} → {to.DisplayName()}";
}
