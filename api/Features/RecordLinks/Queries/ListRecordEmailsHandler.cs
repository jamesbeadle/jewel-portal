using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// Lists one record's tagged emails by delegating to the record-agnostic RecordEmailReader — the
// per-record twin of ListSchedulingEmailsHandler. Returns whatever is tagged now; empty if the
// record is gone, nothing is tagged, or Graph is unconfigured.
public sealed class ListRecordEmailsHandler
    : IQueryHandler<ListRecordEmails, IReadOnlyList<MailboxMessage>>
{
    private readonly RecordEmailReader emails;

    public ListRecordEmailsHandler(RecordEmailReader emails) { this.emails = emails; }

    public Task<IReadOnlyList<MailboxMessage>> HandleAsync(ListRecordEmails query, CancellationToken cancellationToken)
        => emails.ForRecordAsync(query.Type, query.RecordId, cancellationToken);
}
