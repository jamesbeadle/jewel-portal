using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Turn a triaged email into a brand-new request. A new request is created on the chosen
// project, the email body is recorded as the opening inbound shared conversation message, and
// the intake item is marked Linked to the new request. RaisedByEmail is stamped server-side
// from the signed-in triager — the request is raised by Jewel on the sender's behalf.
public sealed record CreateRequestFromIntake(
    string IntakeId,
    string ProjectId,
    RequestType Kind,
    string Reference,
    string Title,
    string Description,
    decimal? Value = null,
    string? RaisedTo = null,
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string RaisedByEmail = "") : ICommand<Request>;
