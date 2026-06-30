using Jewel.JPMS.Contracts.Cqrs;
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
    string OwnerEmail = "") : ICommand<BidPackage>;
