using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Create a new bid package from a tagged mailbox message: creates a Draft package on the chosen
// project and links the originating email to it via the shared record-link tag (the package then reads
// its mail back live by that tag). OwnerEmail is stamped server-side from the signed-in user;
// InternetMessageId lets the link re-find the message if its Graph id has changed.
public sealed record CreateBidPackageFromMessage(
    string MessageId,
    string ProjectId,
    string Title,
    string Trade,
    string? InternetMessageId = null,
    string OwnerEmail = "",
    // How far the record tag spreads across the email's conversation (forwarded verbatim to the
    // shared LinkMessageToRecord path). Default keeps the long-standing anchor+thread-behind
    // sweep; the Control Centre passes an explicit MessageOnly / EntireThread from its
    // "triage the entire thread" checkbox.
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Explicit consent to file the thread under Subcontractor as well as a pathway it already
    // carries. Pre-flighted before the package is created (CrossPathwayGuard), so a rejection
    // creates nothing; the UI's "File under both anyway" re-sends with this true.
    bool AllowCrossPathway = false) : ICommand<BidPackage>;
