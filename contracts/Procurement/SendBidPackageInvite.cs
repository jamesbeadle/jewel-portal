using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Send the tender-invite email for a bid package from the shared mailbox. Every recipient on the
// package goes in BCC (subcontractors must not see each other's addresses); the sent copy is tagged
// with the package's reference ("JPMS/BPI-0001") so replies triaged onto the same tag group under
// the package. The caller composes and reviews Subject/HtmlBody in the UI before sending — this
// command sends exactly what it is given. Moves a Draft package to Inviting. Returns the package.
public sealed record SendBidPackageInvite(
    string BidPackageId,
    string Subject,
    string HtmlBody) : ICommand<BidPackage>;
