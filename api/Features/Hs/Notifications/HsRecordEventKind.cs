namespace Jewel.JPMS.Api.Features.Hs.Notifications;

/// <summary>What happened on a record, as the digest tells it. Persisted as its integer; append, never insert.</summary>
public enum HsRecordEventKind
{
    Raised = 0,
    Commented = 1,
    StatusChanged = 2
}
