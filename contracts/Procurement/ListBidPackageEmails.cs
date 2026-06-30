using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Emails currently tagged to a bid package (read live by tag via the record-link layer). The tag is
// the only association — nothing is stored — so this reflects whatever is tagged to the package now.
public sealed record ListBidPackageEmails(string BidPackageId) : IQuery<IReadOnlyList<MailboxMessage>>;
