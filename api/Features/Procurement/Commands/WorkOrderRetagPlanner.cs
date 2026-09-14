using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>An order as the retag sweep sees it.</summary>
internal sealed record RetagOrder(string WorkOrderId, string ProjectId, string? ProjectRef, int Number, string Reference)
{
    public string LegacyTag => TriageCategories.ForRecord(Reference);
    public string QualifiedTag => TriageCategories.ForRecord(WorkOrderTags.Stem(ProjectRef, ProjectId, Reference));
}

/// <summary>A conversation the audit trail links to an order (RecordLinked / EmailTriaged /
/// EmailSent rows carry both).</summary>
internal sealed record RetagLink(string ConversationId, string WorkOrderId);

/// <summary>The whole tag moves: every email under the legacy tag belongs to the one order.</summary>
internal sealed record WholeTagMove(string LegacyTag, string QualifiedTag);

/// <summary>One conversation moves: the legacy tag names several orders, and the audit trail
/// links this thread to exactly one of them.</summary>
internal sealed record ConversationMove(string LegacyTag, string ConversationId, string QualifiedTag);

/// <summary>A legacy tag several orders share — whatever is still under it after the conversation
/// moves is for a person to file, between these candidates.</summary>
internal sealed record TagCollision(string LegacyTag, IReadOnlyList<string> CandidateTags);

internal sealed record WorkOrderRetagPlan(
    IReadOnlyList<WholeTagMove> WholeTagMoves,
    IReadOnlyList<ConversationMove> ConversationMoves,
    IReadOnlyList<TagCollision> Collisions);

/// <summary>
/// The pure decision behind RetagWorkOrderWorkflowTags: which legacy tags move whole, which threads
/// move on the audit trail's word, and which tags are left with a choice for a person. No Graph, no
/// database — the handler reads the orders and the link rows, this plans, the handler executes.
/// </summary>
internal static class WorkOrderRetagPlanner
{
    public static WorkOrderRetagPlan Plan(IReadOnlyList<RetagOrder> orders, IReadOnlyList<RetagLink> links)
    {
        var wholeTagMoves = new List<WholeTagMove>();
        var conversationMoves = new List<ConversationMove>();
        var collisions = new List<TagCollision>();

        foreach (var sharingANumber in orders.Where(order => order.Number > 0).GroupBy(order => order.Number).OrderBy(group => group.Key))
        {
            var candidates = sharingANumber.ToList();
            if (candidates.Count == 1)
            {
                wholeTagMoves.Add(new WholeTagMove(candidates[0].LegacyTag, candidates[0].QualifiedTag));
                continue;
            }
            conversationMoves.AddRange(ConversationMovesFor(candidates, links));
            collisions.Add(new TagCollision(candidates[0].LegacyTag, candidates.Select(order => order.QualifiedTag).ToList()));
        }

        return new WorkOrderRetagPlan(wholeTagMoves, conversationMoves, collisions);
    }

    private static IEnumerable<ConversationMove> ConversationMovesFor(IReadOnlyList<RetagOrder> candidates, IReadOnlyList<RetagLink> links)
    {
        var ordersById = candidates.ToDictionary(order => order.WorkOrderId, StringComparer.Ordinal);
        return links
            .Where(link => ordersById.ContainsKey(link.WorkOrderId))
            .GroupBy(link => link.ConversationId, StringComparer.Ordinal)
            .Select(thread => (ConversationId: thread.Key, Orders: thread.Select(link => ordersById[link.WorkOrderId]).Distinct().ToList()))
            .Where(thread => thread.Orders.Count == 1)
            .Select(thread => new ConversationMove(thread.Orders[0].LegacyTag, thread.ConversationId, thread.Orders[0].QualifiedTag));
    }
}
