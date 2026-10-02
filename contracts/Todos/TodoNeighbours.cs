using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The to-dos either side of one item in the reader's OPEN pile, in the order the To-dos page
// shows it — what the item's page steps through with Previous / Next, and where "Done & next"
// lands (2026-10-02, Nigel: "close then move on to the next"). The pile is the reader's own list
// (everything for the MD and administrators, the items on their roles for everyone else) with
// the done items taken out; an item that is not in it — already done, or filed away from the
// reader — has nothing before it and the top of the pile after it, so there is always somewhere
// to go while anything is open.
public sealed record TodoNeighbours(TodoItem? Previous, TodoItem? Next)
{
    public static readonly TodoNeighbours None = new(null, null);

    public bool HasPrevious => Previous is not null;
    public bool HasNext => Next is not null;

    public static TodoNeighbours Of(IReadOnlyList<TodoItem> openInOrder, string todoItemId)
    {
        var position = PositionOf(openInOrder, todoItemId);
        if (position < 0) return new TodoNeighbours(null, openInOrder.FirstOrDefault());
        var previous = position > 0 ? openInOrder[position - 1] : null;
        var isLast = position + 1 == openInOrder.Count;
        var next = isLast ? null : openInOrder[position + 1];
        return new TodoNeighbours(previous, next);
    }

    private static int PositionOf(IReadOnlyList<TodoItem> items, string todoItemId)
    {
        for (var i = 0; i < items.Count; i++)
            if (items[i].TodoItemId == todoItemId) return i;
        return -1;
    }
}
