using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Turn a mailbox message into a brand-new request: create the request on the chosen project, record
// the email as the opening inbound shared conversation message, and move the message out of the
// Inbox into the new request's folder. RaisedByEmail is stamped server-side from the signed-in
// triager. InternetMessageId lets the move re-find the message if its Graph id has changed.
// AddToProgramme ("Also add to Programme" on the triage create form) additionally tags the email's
// thread to the project's programme communications (the Scheduling bucket) — exactly what the
// standalone "Tag email to programme" action does — so the email shows under the Programme tab as
// well as on the new request.
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
    string RaisedByEmail = "",
    bool AddToProgramme = false) : ICommand<Request>;
