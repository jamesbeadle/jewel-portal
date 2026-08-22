using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
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
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string? InternetMessageId = null,
    string RaisedByEmail = "",
    bool AddToProgramme = false,
    // How far the request tag (and the Client pathway stamp) spread across the email's
    // conversation -- the same LinkThreadScope as LinkMessageToRecord. The default keeps the
    // long-standing behaviour (anchor + the thread behind it) for existing callers such as
    // "Reply in thread"; the Control Centre passes MessageOnly, or EntireThread when its
    // "triage the entire thread" box is ticked.
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Explicit consent to file the thread under Client as well as a pathway it already carries.
    // Replaces the old hard client wall on this path (removed 2026-08-22, following the
    // 2026-08-21 wall removal on the link path): a request on a Subcontractor/Internal thread is
    // now the same soft "Confirm the cross-filing" as every other dual filing, pre-flighted
    // before the request is created so a rejection creates nothing.
    bool AllowCrossPathway = false) : ICommand<Request>;
