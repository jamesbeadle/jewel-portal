using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

/// <summary>
/// Create one or more to-do items on a project from a mailbox message (live-read model). Mirrors
/// <c>CreateRequestFromMessageHandler</c>: the email is tagged "JPMS/TODO-####" for every item first,
/// verified by read-back, and the rows are only persisted once every tag sticks — so we never create
/// an item whose email is still sitting untagged in the queue. The tag is the only link to the email;
/// no copy is stored.
/// </summary>
public sealed class CreateTodoItemsFromMessageHandler : ICommandHandler<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    public CreateTodoItemsFromMessageHandler(JpmsContext context, IMailboxGraphClient graph, RecordThreadTagger threadTagger)
    { this.context = context; this.graph = graph; this.threadTagger = threadTagger; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(CreateTodoItemsFromMessage command, CancellationToken cancellationToken)
    {
        // A blank ProjectId means GENERAL (company-wide) items — a company-wide email triaged onto
        // the to-do list without belonging to any project. Rows are stored with ProjectId "".
        var projectId = command.ProjectId?.Trim() ?? "";
        if (projectId.Length > 0)
        {
            var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == projectId, cancellationToken);
            if (!projectExists) throw new InvalidOperationException($"Project '{projectId}' not found.");
        }

        var drafts = (command.Items ?? Array.Empty<TodoItemDraft>())
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .ToList();
        if (drafts.Count == 0) throw new InvalidOperationException("At least one to-do item with a title is required.");

        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        var nextNumber = (await context.TodoItems.MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

        var entities = drafts.Select((draft, index) => new TodoItemEntity
        {
            TodoItemId = TodosIdentifierFactory.Next(),
            ProjectId = projectId,
            Number = nextNumber + index,
            Title = Clamp(draft.Title.Trim(), 256),
            Notes = Clamp(draft.Notes?.Trim() ?? "", 2048),
            AssigneeEmail = Clamp(draft.AssigneeEmail?.Trim() ?? "", 256),
            CreatedByEmail = command.CreatedByEmail,
            IsComplete = false,
            CreatedAt = snapshot.ReceivedAt,
            DueAt = draft.DueAt
        }).ToList();

        // Tag the email to every new item first (one "JPMS/TODO-####" category per item, verified by
        // read-back); only persist the rows once every tag sticks. Tag the WHOLE conversation, not just
        // the clicked message (same as LinkMessageToRecordHandler) — otherwise the thread's other emails
        // never gain the JPMS marker and the thread stays in the triage queue. The anchor tag is
        // verified; sibling tagging is best-effort.
        foreach (var entity in entities)
        {
            var tagged = await threadTagger.TagThreadAsync(
                command.MessageId, snapshot.InternetMessageId, snapshot.ConversationId,
                TriageCategories.ForRecord(entity.Reference), cancellationToken);
            if (!tagged)
                throw new InvalidOperationException("The email couldn't be tagged to the new to-do items. Please try again.");
        }

        context.TodoItems.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        return entities.Select(e => e.ToModel()).ToList().AsReadOnly();
    }

    // Email subjects/bodies can exceed the column limits; clamp so a long email can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
