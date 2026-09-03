using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.SiteInstructions;

// Raise a site instruction from a tagged mailbox message — the Control Centre's Internal-pane
// "create new" (2026-09-03), mirroring CreateInventoryItemFromMessage on the Supplier side. The
// instruction itself is exactly an AddSiteInstruction (same fields, same numbering); this
// command additionally links the originating email to the new record via the shared record-link
// tag ("JPMS/SI-####"), so the instruction reads its mail back live like every other record.
// InternetMessageId lets the link re-find the message if its Graph id has changed since the
// queue was rendered.
public sealed record CreateSiteInstructionFromMessage(
    string MessageId,
    string ProjectId,
    string Title,
    string Instruction,
    string Location = "",
    string? InternetMessageId = null,
    // How far the record tag spreads across the email's conversation (forwarded verbatim to the
    // shared LinkMessageToRecord path).
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Carried for parity with the other from-message commands; since 2026-08-28 the pane choice
    // IS the cross-filing decision, so callers send true and the guard is a no-op.
    bool AllowCrossPathway = false) : ICommand<SiteInstruction>;
