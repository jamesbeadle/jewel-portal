using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// Lists a project's scheduling-tagged emails by delegating to the record-agnostic RecordEmailReader
// (RecordType.Scheduling). The scheduling bucket's RecordId IS the project id — one bucket per
// project. Returns whatever is tagged now; empty if Graph is unconfigured.
public sealed class ListSchedulingEmailsHandler
    : IQueryHandler<ListSchedulingEmails, IReadOnlyList<MailboxMessage>>
{
    private readonly RecordEmailReader emails;

    public ListSchedulingEmailsHandler(RecordEmailReader emails) { this.emails = emails; }

    public Task<IReadOnlyList<MailboxMessage>> HandleAsync(ListSchedulingEmails query, CancellationToken cancellationToken)
        => emails.ForRecordAsync(RecordType.Scheduling, query.ProjectId, cancellationToken);
}
