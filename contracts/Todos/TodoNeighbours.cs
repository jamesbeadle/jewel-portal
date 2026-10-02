using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The to-dos either side of one item in the reader's OPEN pile, in the To-dos page's order. An
// item outside the pile (already done) has nothing before it and the top of the pile after it.
public sealed record TodoNeighbours(TodoItem? Previous, TodoItem? Next)
{
    public static readonly TodoNeighbours None = new(null, null);

    public bool HasPrevious => Previous is not null;
    public bool HasNext => Next is not null;
    public string? NextReference => Next?.Reference;

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
