using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record RaiseRequest(
    string ProjectId,
    RequestType Kind,
    string Reference,
    string Title,
    string Description,
    decimal? Value,
    string RaisedByEmail,
    string? RaisedTo = null,
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string? InternalNotes = null,
    string? ClientNotes = null,
    // Backfill support: when logging a historical RFI the issue/response dates and
    // current status are supplied explicitly. Left null for a brand-new request,
    // in which case the handler stamps "now" and opens it.
    DateTimeOffset? RaisedAt = null,
    DateTimeOffset? RespondedAt = null,
    string? ResponseText = null,
    string? RespondedByEmail = null,
    RequestStatus? Status = null,
    // EOT only: the Notice of Delay this EOT arises from. Optional — an EOT can stand alone.
    string? RelatedNodRequestId = null,
    // The ball-in-court party picked from the project's contact list (Setup tab). When set, the
    // server verifies the contact belongs to the project and derives the RaisedTo display string
    // from it — any RaisedTo passed alongside is ignored. Null keeps RaisedTo as free text
    // (legacy rows and non-dropdown callers such as triage).
    string? RaisedToContactId = null) : ICommand<Request>;
