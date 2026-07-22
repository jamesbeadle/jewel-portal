using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Records that an invited subcontractor has declined to tender ("rang them — they're too busy"),
// or undoes that (Declined = false) when it was recorded in error. Undoing restores Responded when
// the subcontractor has a live quote on the package, otherwise Invited. The winning recipient can't
// be declined — re-award first. Returns the package's full recipient list.
public sealed record DeclineBidPackageRecipient(
    string BidPackageId,
    string RecipientId,
    bool Declined = true) : ICommand<IReadOnlyList<BidPackageRecipient>>;
