using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Raise a work order from a tagged mailbox message — the Control Centre's "create new" for the
// Subcontractor pathway, alongside CreateBidPackageFromMessage. The order itself is exactly a
// CreateManualWorkOrder (same lines/programme/deposit/draft semantics, same validation and
// numbering); this command additionally links the originating email to the new order via the
// shared record-link tag, so the order reads its mail back live like every other record.
// RaisedByEmail is stamped server-side from the signed-in user; InternetMessageId lets the link
// re-find the message if its Graph id has changed since the queue was rendered.
public sealed record CreateWorkOrderFromMessage(
    string MessageId,
    string ProjectId,
    string SubcontractorId,
    string Title,
    string Scope,
    IReadOnlyList<ManualWorkOrderLine> Lines,
    DateTimeOffset? ProgrammeStart = null,
    DateTimeOffset? TargetCompletion = null,
    string ProgrammeNotes = "",
    bool SaveAsDraft = false,
    bool DepositRequired = false,
    decimal? DepositPercent = null,
    string? InternetMessageId = null,
    string RaisedByEmail = "",
    // Graph attachment ids ticked in the triage form: each is copied off the email into the new
    // order's attachments (record keeping only -- never sent to the supplier). The bytes move
    // server-side, mailbox -> blob store, so they never round-trip through the browser. The
    // handler downloads them all BEFORE the order is created, so a vanished attachment fails
    // the apply cleanly rather than leaving a half-attached order behind.
    IReadOnlyList<string>? AttachmentIds = null,
    // How far the record tag spreads across the email's conversation (forwarded verbatim to the
    // shared LinkMessageToRecord path). Default keeps the long-standing anchor+thread-behind
    // sweep; the Control Centre passes an explicit MessageOnly / EntireThread from its
    // "triage the entire thread" checkbox. Named LinkScope — not Scope like the sibling
    // create-from-message commands — because this command already carries the ORDER's Scope
    // (the instructed works text) and a record can't have two parameters with one name.
    LinkThreadScope LinkScope = LinkThreadScope.ThreadBehindAnchor) : ICommand<WorkOrder>;
