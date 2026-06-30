using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

// Lists a bid package's tagged emails by delegating to the record-agnostic RecordEmailReader
// (RecordType.BidPackageInvite). Returns whatever is tagged now; empty if Graph is unconfigured.
public sealed class ListBidPackageEmailsHandler
    : IQueryHandler<ListBidPackageEmails, IReadOnlyList<MailboxMessage>>
{
    private readonly RecordEmailReader emails;

    public ListBidPackageEmailsHandler(RecordEmailReader emails) { this.emails = emails; }

    public Task<IReadOnlyList<MailboxMessage>> HandleAsync(ListBidPackageEmails query, CancellationToken cancellationToken)
        => emails.ForRecordAsync(RecordType.BidPackageInvite, query.BidPackageId, cancellationToken);
}
