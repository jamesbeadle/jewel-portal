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

/// <summary>
/// A published staff document requiring acknowledgement (NDA, policy, H&S doc): the revision's
/// declaration (the standard line when its publisher wrote none) and the PDF people read, when attached.
/// </summary>
public sealed record PolicyDocument(
    string PolicyDocumentId,
    string Title,
    string Summary,
    int Revision,
    string PublishedByEmail,
    DateTimeOffset PublishedAt,
    bool IsActive,
    int SignedCount,
    int OutstandingCount,
    string Declaration = PolicyDeclarations.Standard,
    string FileName = "")
{
    public bool HasFile => FileName.Length > 0;
}

/// <summary>
/// One recipient's acknowledgement state for one policy revision — signed on their own portal login,
/// or through a Policy sign-off form link (FormInviteId), which also records who they are with and
/// their position, and the form they sent (FormSubmissionId).
/// </summary>
public sealed record PolicySignOff(
    string PolicySignOffId,
    string PolicyDocumentId,
    string Title,
    string Summary,
    int Revision,
    string RecipientEmail,
    DateTimeOffset RequestedAt,
    DateTimeOffset? SignedAt,
    string SignedName,
    string RecipientName = "",
    string CompanyName = "",
    string Position = "",
    string? FormInviteId = null,
    string? FormSubmissionId = null,
    bool HasFile = false)
{
    public bool IsSigned => SignedAt is not null;

    public bool IsByLink => FormInviteId is not null;
}
