using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos;

// The one rule about how a to-do ↔ to-do link is stored: an undirected pair, held as ONE row with
// the two ids in canonical (ordinal) order. Every writer normalises through here, so "already
// linked?" is a single indexed equality check and A→B / B→A can never exist as two rows.
internal static class TodoItemLinkPairs
{
    public static (string AId, string BId) Normalise(string firstId, string secondId) =>
        string.CompareOrdinal(firstId, secondId) <= 0 ? (firstId, secondId) : (secondId, firstId);

    // Every link row that names the given item, either side.
    public static IQueryable<TodoItemLinkEntity> Touching(this JpmsContext context, string todoItemId) =>
        context.TodoItemLinks.Where(link => link.TodoItemAId == todoItemId || link.TodoItemBId == todoItemId);

    // The OTHER item of every link touching the given item, as entities in no particular order —
    // callers apply TodosOrdering.InListOrder like every other list read.
    public static async Task<List<TodoItemEntity>> LinkedItemsForAsync(
        this JpmsContext context, string todoItemId, CancellationToken cancellationToken)
    {
        var otherIds = await context.Touching(todoItemId)
            .Select(link => link.TodoItemAId == todoItemId ? link.TodoItemBId : link.TodoItemAId)
            .ToListAsync(cancellationToken);
        if (otherIds.Count == 0) return new List<TodoItemEntity>();
        return await context.TodoItems.AsNoTracking()
            .Where(item => otherIds.Contains(item.TodoItemId))
            .ToListAsync(cancellationToken);
    }
}
