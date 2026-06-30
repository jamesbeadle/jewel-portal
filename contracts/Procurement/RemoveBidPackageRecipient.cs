using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Remove one invited subcontractor from a bid package. Returns the package's remaining recipients.
public sealed record RemoveBidPackageRecipient(
    string BidPackageId,
    string RecipientId) : ICommand<IReadOnlyList<BidPackageRecipient>>;
