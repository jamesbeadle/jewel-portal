using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Turn a mailbox message into a brand-new request: create the request on the chosen project, record
// the email as the opening inbound shared conversation message, and move the message out of the
// Inbox into the new request's folder. RaisedByEmail is stamped server-side from the signed-in
// triager. InternetMessageId lets the move re-find the message if its Graph id has changed.
public sealed record CreateRequestFromMessage(
    string MessageId,
    string ProjectId,
    RequestType Kind,
    string Reference,
    string Title,
    string Description,
    decimal? Value = null,
    string? RaisedTo = null,
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string? InternetMessageId = null,
    string RaisedByEmail = "") : ICommand<Request>;
