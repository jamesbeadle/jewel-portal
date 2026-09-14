using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Migration sweep from legacy flat work-order tags ("JPMS/WO-0045") to project-qualified ones
/// ("JPMS/JBB-2026-001-WO-0045") — the work-order twin of RetagRequestWorkflowTagsHandler, with one
/// more step because order numbers collide across projects: WorkOrderRetagPlanner decides which tags
/// move whole, which threads move on the audit trail's word (the RecordLinked / EmailTriaged /
/// EmailSent rows name the order and the conversation), and which are left for a person. Every
/// move adds the qualified tag BEFORE removing the legacy one, so an email never loses its marker
/// mid-move. Failures are logged and counted, never fatal — re-running picks up what a transient
/// Graph failure left behind.
/// </summary>
public sealed class RetagWorkOrderWorkflowTagsHandler : ICommandHandler<RetagWorkOrderWorkflowTags, WorkOrderRetagSummary>
{
    private static readonly int[] LinkEventTypes =
    {
        (int)AuditEventType.EmailTriaged,
        (int)AuditEventType.RecordLinked,
        (int)AuditEventType.RecordCreatedFromEmail,
        (int)AuditEventType.EmailSent,
        (int)AuditEventType.DraftCreated
    };

    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly ILogger<RetagWorkOrderWorkflowTagsHandler> logger;

    public RetagWorkOrderWorkflowTagsHandler(JpmsContext context, IMailboxGraphClient graph, ILogger<RetagWorkOrderWorkflowTagsHandler> logger)
    {
        this.context = context;
        this.graph = graph;
        this.logger = logger;
    }

    public async Task<WorkOrderRetagSummary> HandleAsync(RetagWorkOrderWorkflowTags command, CancellationToken cancellationToken)
    {
        var plan = WorkOrderRetagPlanner.Plan(await OrdersAsync(cancellationToken), await LinksAsync(cancellationToken));

        var moved = 0;
        var failures = 0;
        foreach (var move in plan.ConversationMoves)
        {
            try { moved += await MoveConversationAsync(move, cancellationToken); }
            catch (Exception exception) { failures++; LogFailure(exception, move.LegacyTag, move.QualifiedTag); }
        }
        foreach (var move in plan.WholeTagMoves)
        {
            try { moved += await graph.RetagAsync(move.LegacyTag, move.QualifiedTag, cancellationToken); }
            catch (Exception exception) { failures++; LogFailure(exception, move.LegacyTag, move.QualifiedTag); }
        }

        var leftovers = new List<WorkOrderRetagLeftover>();
        foreach (var collision in plan.Collisions)
            leftovers.AddRange(await LeftoversAsync(collision, cancellationToken));

        var processed = plan.WholeTagMoves.Count + plan.Collisions.Count;
        logger.LogInformation(
            "Work-order retag sweep complete: {Processed} tag(s) processed, {Moved} email(s) moved, {Left} thread(s) left for a person, {Failures} failure(s).",
            processed, moved, leftovers.Count, failures);
        return new WorkOrderRetagSummary(processed, moved, failures, leftovers);
    }

    private async Task<IReadOnlyList<RetagOrder>> OrdersAsync(CancellationToken cancellationToken)
    {
        var projectRefs = await context.Projects.AsNoTracking()
            .ToDictionaryAsync(project => project.ProjectId, project => project.Reference, cancellationToken);
        var orders = await context.WorkOrders.AsNoTracking()
            .Where(order => order.Number > 0)
            .ToListAsync(cancellationToken);
        return orders
            .Select(order => new RetagOrder(order.WorkOrderId, order.ProjectId, projectRefs.GetValueOrDefault(order.ProjectId), order.Number, order.Reference))
            .ToList();
    }

    private async Task<IReadOnlyList<RetagLink>> LinksAsync(CancellationToken cancellationToken)
    {
        var rows = await context.AuditEvents.AsNoTracking()
            .Where(row => row.RecordType == (int)RecordType.WorkOrder
                          && row.RecordId != null
                          && row.ConversationId != null
                          && LinkEventTypes.Contains(row.EventType))
            .Select(row => new { row.ConversationId, row.RecordId })
            .Distinct()
            .ToListAsync(cancellationToken);
        return rows.Select(row => new RetagLink(row.ConversationId!, row.RecordId!)).ToList();
    }

    // The qualified tag goes on first so the marker never lapses (RetagAsync makes the same
    // promise mailbox-wide); the legacy tag comes off only from a message that took the new one.
    private async Task<int> MoveConversationAsync(ConversationMove move, CancellationToken cancellationToken)
    {
        var moved = 0;
        foreach (var messageId in await graph.ListTaggedIdsInConversationAsync(move.ConversationId, move.LegacyTag, cancellationToken))
        {
            if (!await graph.AssignAsync(messageId, null, move.QualifiedTag, cancellationToken)) continue;
            if (await graph.RemoveTagAsync(messageId, null, move.LegacyTag, cancellationToken)) moved++;
        }
        return moved;
    }

    private async Task<IReadOnlyList<WorkOrderRetagLeftover>> LeftoversAsync(TagCollision collision, CancellationToken cancellationToken)
    {
        var stillTagged = await graph.ListByTagAsync(collision.LegacyTag, cursor: null, take: 200, cancellationToken);
        return stillTagged.Items
            .GroupBy(message => string.IsNullOrEmpty(message.ConversationId) ? message.Id : message.ConversationId, StringComparer.Ordinal)
            .Select(thread => new WorkOrderRetagLeftover(collision.LegacyTag, thread.First().Subject, collision.CandidateTags))
            .ToList();
    }

    private void LogFailure(Exception exception, string legacyTag, string qualifiedTag) =>
        logger.LogWarning(exception, "Work-order retag sweep: {OldTag} -> {NewTag} failed; run the sweep again to catch it up.", legacyTag, qualifiedTag);
}
