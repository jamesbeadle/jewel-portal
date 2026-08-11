using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The to-do items linked to one item, derived from its tagged mail: every OTHER "JPMS/TODO-####"
// tag carried by any email tagged to this item names a linked item. That makes the link exactly
// what the Control Centre set up — one email raised as (or filed to) several items ties them all
// together — and keeps it live: tag an email to another item later and the link appears, untag
// the last shared email and it goes. Nothing is stored, mirroring the linked-emails read itself.
public sealed class ListLinkedTodoItemsHandler : IQueryHandler<ListLinkedTodoItems, IReadOnlyList<TodoItem>>
{
    private const string TodoTagPrefix = TriageCategories.WorkflowPrefix + "TODO-";

    private readonly JpmsContext context;
    private readonly RecordEmailReader emails;

    public ListLinkedTodoItemsHandler(JpmsContext context, RecordEmailReader emails)
    {
        this.context = context;
        this.emails = emails;
    }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListLinkedTodoItems query, CancellationToken cancellationToken)
    {
        var ownReference = await context.TodoItems.AsNoTracking()
            .Where(item => item.TodoItemId == query.TodoItemId)
            .Select(item => item.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        if (ownReference is null) return Array.Empty<TodoItem>();

        var taggedMail = await emails.ForRecordAsync(RecordType.Todo, query.TodoItemId, cancellationToken);
        var linkedReferences = taggedMail
            .SelectMany(email => email.Categories)
            .Where(category => category.StartsWith(TodoTagPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(category => category[TriageCategories.WorkflowPrefix.Length..])
            .Where(reference => !reference.Equals(ownReference, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (linkedReferences.Count == 0) return Array.Empty<TodoItem>();

        var entities = await context.TodoItems.AsNoTracking()
            .Where(item => linkedReferences.Contains(item.Reference))
            .ToListAsync(cancellationToken);
        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        return entities
            .InListOrder()
            .Select(entity => entity.ToModel(personNames))
            .ToList()
            .AsReadOnly();
    }
}
