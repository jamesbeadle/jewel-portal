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
///
/// When <c>LinkRequestId</c> is set, the email is ALSO tagged to that request (same tag mechanism as
/// <c>LinkMessageToRecordHandler</c>, same guards: the request must exist, live on the same project
/// and not be Closed — which also rules out general, no-project batches). The request tag is applied
/// and verified before any to-do tag, so a failed link creates nothing.
/// </summary>
public sealed class CreateTodoItemsFromMessageHandler : ICommandHandler<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly RecordProviderRegistry providers;
    public CreateTodoItemsFromMessageHandler(JpmsContext context, IMailboxGraphClient graph, RecordThreadTagger threadTagger, RecordProviderRegistry providers)
    { this.context = context; this.graph = graph; this.threadTagger = threadTagger; this.providers = providers; }

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

        // Optional request link ("Create new → To-do" with a request picked): tag the whole thread to
        // the request as well, exactly as "Link to existing" would — the email then feeds the request's
        // conversation AND the to-dos created below. Applied and verified first, so a failed request
        // link leaves the queue untouched (no items, no tags).
        if (!string.IsNullOrWhiteSpace(command.LinkRequestId))
        {
            if (projectId.Length == 0)
                throw new InvalidOperationException(
                    "General to-do items can't be linked to a request — every request belongs to a project. Choose the request's project instead.");

            var request = await providers.For(RecordType.Request).FindAsync(command.LinkRequestId, cancellationToken)
                ?? throw new InvalidOperationException($"Request '{command.LinkRequestId}' not found.");

            if (!string.Equals(request.ProjectId, projectId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"{request.Reference} belongs to a different project, so the email can't be linked to it alongside these to-do items.");

            // Same guard as LinkMessageToRecordHandler: a closed request can't receive new triage
            // emails. The picker already hides closed requests; this protects the command path itself.
            if (string.Equals(request.StatusLabel, nameof(RequestStatus.Closed), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"{request.Reference} is closed, so this email can't be linked to it. Reopen the request first, or create the to-dos without a request link.");

            var requestTagged = await threadTagger.TagThreadAsync(
                command.MessageId, snapshot.InternetMessageId, snapshot.ConversationId,
                TriageCategories.ForRecord(request.TagReference), cancellationToken);
            if (!requestTagged)
                throw new InvalidOperationException("The email couldn't be tagged to the request. Please try again.");
        }

        var nextNumber = (await context.TodoItems.MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

        var entities = drafts.Select((draft, index) => new TodoItemEntity
        {
            TodoItemId = TodosIdentifierFactory.Next(),
            ProjectId = projectId,
            Number = nextNumber + index,
            Title = Clamp(draft.Title.Trim(), 256),
            Notes = Clamp(draft.Notes?.Trim() ?? "", 2048),
            AssigneeRole = (int?)draft.AssigneeRole,
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

        // Pathway (docs/Pathway-Split-Platform-Flow-Plan.md §2.1): a to-do link is NEUTRAL — it never
        // sets or changes a pathway on a thread that already has one (a to-do raised from a client
        // email leaves the thread Client). Only when the triager worked down the Internal pathway AND
        // the thread has no pathway yet is it filed under Internal. Best-effort: the to-do tags are
        // the primary association; a missed stamp is healed by the backfill.
        var wantsInternal = string.Equals(command.Pathway?.Trim(), "Internal", StringComparison.OrdinalIgnoreCase);
        var hasBucket = (snapshot.Categories ?? Array.Empty<string>()).Any(TriageCategories.IsBucketTag);
        if (wantsInternal && !hasBucket)
        {
            try
            {
                await threadTagger.TagThreadAsync(
                    command.MessageId, snapshot.InternetMessageId, snapshot.ConversationId,
                    TriageCategories.Internal, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* best-effort */ }
        }

        return entities.Select(e => e.ToModel()).ToList().AsReadOnly();
    }

    // Email subjects/bodies can exceed the column limits; clamp so a long email can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
