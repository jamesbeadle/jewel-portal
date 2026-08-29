using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Inventory;

// Add an inventory item from a tagged mailbox message — the Control Centre's first "create new"
// for the Supplier pathway, mirroring CreateDefectFromMessage on the Subcontractor side. The item
// itself is exactly an AddInventoryItem (same fields, same numbering); this command additionally
// links the originating email to the new item via the shared record-link tag ("JPMS/INV-####"),
// so the item reads its mail back live like every other record. InternetMessageId lets the link
// re-find the message if its Graph id has changed since the queue was rendered.
public sealed record CreateInventoryItemFromMessage(
    string MessageId,
    string ProjectId,
    string ProductName,
    string ProductDetails = "",
    string Location = "",
    string LocationDetails = "",
    string? InternetMessageId = null,
    // How far the record tag spreads across the email's conversation (forwarded verbatim to the
    // shared LinkMessageToRecord path).
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Carried for parity with the other from-message commands; since 2026-08-28 the pane choice
    // IS the cross-filing decision, so callers send true and the guard is a no-op.
    bool AllowCrossPathway = false) : ICommand<InventoryItem>;
