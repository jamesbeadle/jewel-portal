using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Invite one or more subcontractors to tender for a bid package. Idempotent per (package,
// subcontractor): already-invited subcontractors are left as-is. Returns the package's full recipient
// list. Moves a Draft package to Inviting.
public sealed record InviteSubcontractorsToBidPackage(
    string BidPackageId,
    IReadOnlyList<string> SubcontractorIds) : ICommand<IReadOnlyList<BidPackageRecipient>>;
