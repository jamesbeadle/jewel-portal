using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

// Raise a defect from a tagged mailbox message — the Control Centre's third "create new" for the
// Subcontractor pathway, alongside CreateBidPackageFromMessage and CreateWorkOrderFromMessage.
// The defect itself is exactly a RaiseDefect (same fields, same numbering); this command
// additionally links the originating email to the new defect via the shared record-link tag
// ("JPMS/DEF-####"), so the defect reads its mail back live like every other record.
// InternetMessageId lets the link re-find the message if its Graph id has changed since the
// queue was rendered.
public sealed record CreateDefectFromMessage(
    string MessageId,
    string ProjectId,
    string Description,
    string Location = "",
    string AssignedToEmail = "",
    string? InternetMessageId = null,
    // How far the record tag spreads across the email's conversation (forwarded verbatim to the
    // shared LinkMessageToRecord path). Default keeps the long-standing anchor+thread-behind
    // sweep; the Control Centre passes an explicit MessageOnly / EntireThread from its
    // "triage the entire thread" checkbox.
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Explicit consent to file the thread under Subcontractor as well as a pathway it already
    // carries. Pre-flighted before the defect is created (CrossPathwayGuard), so a rejection
    // creates nothing; the UI's "File under both anyway" re-sends with this true.
    bool AllowCrossPathway = false) : ICommand<Defect>;
