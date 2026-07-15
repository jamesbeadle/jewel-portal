using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record UpdateRequestDetails(
    string RequestId,
    string Reference,
    string Title,
    string Description,
    RequestStatus Status,
    decimal? Value,
    string? ResponseText,
    string? RespondedByEmail,
    bool ImpliesVariation,
    string? RaisedTo = null,
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string? RelatedDrawingSpec = null,
    string? InternalNotes = null,
    string? ClientNotes = null,
    DateTimeOffset? RaisedAt = null,
    // EOT only: the Notice of Delay this EOT arises from. Optional — an EOT can stand alone.
    string? RelatedNodRequestId = null,
    // The date the request closed (today or earlier). Only meaningful when Status is Closed: a
    // value corrects the recorded close date; null means "not supplied" and keeps the existing one.
    DateTimeOffset? ClosedAt = null,
    // When the official document was issued to the correspondent. User-set and user-updated; a
    // value (over)writes the recorded issue date, null means "not supplied" and keeps the existing
    // one (most edit surfaces don't carry it).
    DateTimeOffset? IssuedAt = null) : ICommand<Request>;
