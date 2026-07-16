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
    DateTimeOffset? IssuedAt = null,
    // The ball-in-court party picked from the project's contact list (Setup tab). When set, the
    // server verifies the contact belongs to the project and derives the RaisedTo display string
    // from it — RaisedTo passed alongside is ignored. Null clears the link and keeps whatever
    // RaisedTo string was supplied (legacy free text survives edit round-trips this way).
    string? RaisedToContactId = null,
    // Critical Path tag — the RFI is programme-related (its answer gates critical-path work).
    // A value (over)writes the tag; null means "not supplied" and keeps the existing one (most
    // edit surfaces don't carry it, so status changes and the like never shed the tag).
    bool? CriticalPath = null) : ICommand<Request>;
