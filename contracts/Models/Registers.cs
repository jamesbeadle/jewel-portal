namespace Jewel.JPMS.Models;

/// <summary>
/// The company registers that replace the Monday boards
/// (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §8). One register pattern — item,
/// counterparty, key dates, cost, owner — presented per kind, because the kinds differ in what
/// expires: an insurance renews, a subscription bills, a van has an MOT and tax.
/// </summary>
public enum RegisterKind
{
    Insurance = 0,
    Subscription = 1,
    Van = 2,
    TradeAccount = 3,
}

/// <summary>
/// One register row. Field meaning shifts with the kind and the UI labels accordingly:
/// Insurance — Counterparty=insurer, Reference=policy no, KeyDate=renewal;
/// Subscription — Counterparty=provider, KeyDate=next renewal, SecondaryDate=cancellation notice by;
/// Van — Counterparty=assigned driver, Reference=registration, KeyDate=MOT due, SecondaryDate=tax due;
/// Trade account — Counterparty=merchant, Reference=account no, KeyDate=review date.
/// </summary>
public sealed record RegisterItem(
    string RegisterItemId,
    RegisterKind Kind,
    string Name,
    string Counterparty,
    string Reference,
    string OwnerEmail,
    decimal Cost,
    string BillingCycle,
    DateTimeOffset? KeyDate,
    DateTimeOffset? SecondaryDate,
    string Notes,
    bool IsActive);

/// <summary>A published staff document requiring acknowledgement (NDA, policy, H&S doc).</summary>
public sealed record PolicyDocument(
    string PolicyDocumentId,
    string Title,
    string Summary,
    int Revision,
    string PublishedByEmail,
    DateTimeOffset PublishedAt,
    bool IsActive,
    int SignedCount,
    int OutstandingCount);

/// <summary>One recipient's acknowledgement state for one policy revision.</summary>
public sealed record PolicySignOff(
    string PolicySignOffId,
    string PolicyDocumentId,
    string Title,
    string Summary,
    int Revision,
    string RecipientEmail,
    DateTimeOffset RequestedAt,
    DateTimeOffset? SignedAt,
    string SignedName);
