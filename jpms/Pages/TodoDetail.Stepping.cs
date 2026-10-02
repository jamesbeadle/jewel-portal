using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

// Stepping through the pile from the item's page (2026-10-02, Nigel: "close then move on to the
// next"): Previous / Next walk the reader's open to-dos in the order the To-dos page shows them,
// and Done & next closes this one and lands on the one after it — the list page is never the
// stop in between. The pile is read once, with the item, so the neighbours are the ones Nigel
// saw when he arrived; the router keys pages by route value, so landing on the next item re-runs
// the whole load and reads the pile afresh.
public partial class TodoDetail
{
    private TodoNeighbours neighbours = TodoNeighbours.None;

    private bool CanStepOn => item is not null && !item.IsComplete && neighbours.HasNext;

    // A failed read leaves the stepper hidden — the page's own job is untouched.
    private async Task LoadNeighboursAsync()
    {
        try
        {
            var newestFirst = await ViewStorage.ReadNewestFirstAsync(Auth.CurrentUser!.Email);
            var pile = CanSeeAll ? await TodoStore.ListAllAsync() : await TodoStore.ListMineAsync();
            var openInOrder = TodoSortOrder.Apply(pile, newestFirst).Where(todo => !todo.IsComplete).ToList();
            neighbours = TodoNeighbours.Of(openInOrder, TodoItemId);
        }
        catch { neighbours = TodoNeighbours.None; }
    }

    private void OpenNeighbour(TodoItem neighbour) => Nav.NavigateTo($"/todos/{neighbour.TodoItemId}");

    // The destination is taken before the close: once this item is done it leaves the pile.
    private async Task CompleteAndStepOnAsync()
    {
        if (item is null || item.IsComplete) return;
        var destination = neighbours.Next;
        await RunAsync(CompleteCommand(true));
        if (error is not null) return;
        if (destination is null) { Nav.NavigateTo("/todos"); return; }
        OpenNeighbour(destination);
    }
}
