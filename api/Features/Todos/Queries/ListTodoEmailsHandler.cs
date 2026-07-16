using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// Lists a to-do item's linked emails by delegating to the record-agnostic RecordEmailReader
// (RecordType.Todo). The item's sequential TODO-#### reference is its tag stem, so this returns
// whatever is tagged "JPMS/TODO-####" right now — general (no-project) items included, since the
// provider finds items by id alone. Empty if the item is gone or Graph is unconfigured.
public sealed class ListTodoEmailsHandler : IQueryHandler<ListTodoEmails, IReadOnlyList<MailboxMessage>>
{
    private readonly RecordEmailReader emails;

    public ListTodoEmailsHandler(RecordEmailReader emails) { this.emails = emails; }

    public Task<IReadOnlyList<MailboxMessage>> HandleAsync(ListTodoEmails query, CancellationToken cancellationToken)
        => emails.ForRecordAsync(RecordType.Todo, query.TodoItemId, cancellationToken);
}
