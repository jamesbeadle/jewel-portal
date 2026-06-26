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
    RequestStatus? Status = null) : ICommand<Request>;
