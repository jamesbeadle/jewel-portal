using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Todos;
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
    private readonly TodoActivityRecorder activity;
    public CreateTodoItemsFromMessageHandler(JpmsContext context, IMailboxGraphClient graph, RecordThreadTagger threadTagger, RecordProviderRegistry providers, TodoActivityRecorder activity)
    { this.context = context; this.graph = graph; this.threadTagger = threadTagger; this.providers = providers; this.activity = activity; }

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

        // The command's Scope decides how far every tag below spreads across the conversation,
        // exactly as in LinkMessageToRecordHandler: MessageOnly suppresses the sibling sweep (the
        // thread's other emails keep queueing); EntireThread drops the received-at cutoff; the
        // default sweeps the anchor plus the thread behind it. One scope governs the request link,
        // the per-item TODO tags and the pathway stamp alike — they are one triage decision.
        var sweepConversationId = command.Scope == LinkThreadScope.MessageOnly ? null : snapshot.ConversationId;
        DateTimeOffset? sweepCutoff = command.Scope == LinkThreadScope.EntireThread ? null : snapshot.ReceivedAt;

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
                command.MessageId, snapshot.InternetMessageId, sweepConversationId,
                TriageCategories.ForRecord(request.TagReference), cancellationToken,
                anchorReceivedAt: sweepCutoff);
            if (!requestTagged)
                throw new InvalidOperationException("The email couldn't be tagged to the request. Please try again.");
        }

        // FAN-OUT: a row may name several assignees — roles, each optionally pinned to a named
        // holder — and each one becomes its own item: same title, detail and due date, but its own
        // TODO-#### reference, its own tag on this email and its own tick-box. An internal email
        // that needs the QS to price something and the site manager to book the access is two
        // to-dos raised in one action, and either can be completed without closing the other. A row
        // with no assignees stays a single unassigned item. Duplicate assignees on one row
        // collapse, so the triager can't accidentally raise the same item twice.
        var expanded = TodoItemDrafts.FanOutByAssignee(drafts);

        // Every pinned person must currently hold the role they are pinned with — checked before
        // any email is tagged, so a bad pin creates nothing.
        foreach (var assignee in expanded
                     .Where(item => item.AssigneePersonEmail is not null)
                     .Select(item => item.Assignee!)
                     .DistinctBy(a => (a.Role, a.PersonEmail!.ToLowerInvariant())))
        {
            await TodoAssigneeGuard.EnsurePersonHoldsRoleAsync(
                context, assignee.Role, assignee.PersonEmail, cancellationToken);
        }

        // Every LINKED to-do named on a row must exist — checked before any email is tagged, so a
        // stale pick (an item deleted since the picker loaded) creates nothing rather than raising
        // items whose promised links silently point nowhere.
        static List<string> CleanLinkedIds(TodoItemDraft draft) =>
            (draft.LinkedTodoItemIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        var allLinkedIds = drafts.SelectMany(CleanLinkedIds).Distinct(StringComparer.Ordinal).ToList();
        if (allLinkedIds.Count > 0)
        {
            var known = await context.TodoItems.AsNoTracking()
                .Where(item => allLinkedIds.Contains(item.TodoItemId))
                .Select(item => item.TodoItemId)
                .ToListAsync(cancellationToken);
            var missing = allLinkedIds.Except(known, StringComparer.Ordinal).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "A to-do item picked as a link no longer exists — remove it from the draft and apply again.");
        }

        var nextNumber = (await context.TodoItems.MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

        var fannedOut = expanded.Select((item, index) => (item.Draft, Entity: new TodoItemEntity
        {
            TodoItemId = TodosIdentifierFactory.Next(),
            ProjectId = projectId,
            Number = nextNumber + index,
            Title = Clamp(item.Draft.Title.Trim(), 256),
            Notes = Clamp(item.Draft.Notes?.Trim() ?? "", 2048),
            AssigneeRole = (int?)item.AssigneeRole,
            AssigneePersonEmail = TodoAssigneeGuard.NormalisePersonEmail(item.AssigneePersonEmail),
            CreatedByEmail = command.CreatedByEmail,
            IsComplete = false,
            CreatedAt = snapshot.ReceivedAt,
            DueAt = item.Draft.DueAt
        })).ToList();
        var entities = fannedOut.Select(pair => pair.Entity).ToList();

        // Tag the email to every new item first (one "JPMS/TODO-####" category per item, verified by
        // read-back); only persist the rows once every tag sticks. The spread across the
        // conversation follows command.Scope (see above). The anchor tag is verified; sibling
        // tagging is best-effort.
        foreach (var entity in entities)
        {
            var tagged = await threadTagger.TagThreadAsync(
                command.MessageId, snapshot.InternetMessageId, sweepConversationId,
                TriageCategories.ForRecord(entity.Reference), cancellationToken,
                anchorReceivedAt: sweepCutoff);
            if (!tagged)
                throw new InvalidOperationException("The email couldn't be tagged to the new to-do items. Please try again.");
        }

        context.TodoItems.AddRange(entities);
        foreach (var entity in entities)
            activity.Record(entity, TodoActivityKind.Created,
                TodoActivitySummaries.CreatedFromEmailSummary(entity, snapshot.Subject), command.CreatedByEmail);

        // The row's LINKED to-dos (validated above): every item the row fanned out into is linked
        // to every existing item picked — undirected pairs in canonical order, one row each
        // (TodoItemLinkPairs), saved with the items so a failure leaves neither behind.
        foreach (var (draft, entity) in fannedOut)
        {
            foreach (var linkedId in CleanLinkedIds(draft))
            {
                var (aId, bId) = TodoItemLinkPairs.Normalise(entity.TodoItemId, linkedId);
                context.TodoItemLinks.Add(new TodoItemLinkEntity
                {
                    TodoItemLinkId = TodosIdentifierFactory.Next(),
                    TodoItemAId = aId,
                    TodoItemBId = bId,
                    LinkedAt = DateTimeOffset.UtcNow,
                    LinkedByEmail = command.CreatedByEmail
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Pathway (docs/Pathway-Split-Platform-Flow-Plan.md §2.1): a to-do link is NEUTRAL — it never
        // sets or changes a pathway on a thread that already has one (a to-do raised from a client
        // email leaves the thread Client). Only when the triager worked down a NON-CLIENT pathway AND
        // the thread has no pathway yet is it filed there — Internal for company admin, Subcontractor
        // for "chase this supplier for their H&S file", which is subcontract correspondence that
        // happens to be captured as a to-do. Client is never stamped from here: the wall is only ever
        // crossed into by an explicit client record (a request link does that, and withholds Pathway).
        // Best-effort: the to-do tags are the primary association; a missed stamp is healed by the
        // backfill.
        var requested = command.Pathway?.Trim();
        var bucket = string.Equals(requested, "Internal", StringComparison.OrdinalIgnoreCase)
            ? TriageCategories.Internal
            : string.Equals(requested, "Subcontractor", StringComparison.OrdinalIgnoreCase)
                ? TriageCategories.Subcontractor
                : null;
        var hasBucket = (snapshot.Categories ?? Array.Empty<string>()).Any(TriageCategories.IsBucketTag);
        if (bucket is not null && !hasBucket)
        {
            try
            {
                await threadTagger.TagThreadAsync(
                    command.MessageId, snapshot.InternetMessageId, sweepConversationId,
                    bucket, cancellationToken, anchorReceivedAt: sweepCutoff);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* best-effort */ }
        }

        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        return entities.Select(e => e.ToModel(personNames)).ToList().AsReadOnly();
    }

    // Email subjects/bodies can exceed the column limits; clamp so a long email can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
