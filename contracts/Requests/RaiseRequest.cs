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
    string? ClientNotes = null) : ICommand<Request>;
