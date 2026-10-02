using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

// Previous / Next through the reader's open pile, and Done & next (2026-10-02, Nigel's ask).
public partial class TodoDetail
{
    private TodoNeighbours neighbours = TodoNeighbours.None;

    private bool CanStepOn => item is not null && !item.IsComplete && neighbours.HasNext;

    private async Task LoadNeighboursAsync()
    {
        try
        {
            var reader = Auth.CurrentUser!;
            var newestFirst = await ViewStorage.ReadNewestFirstAsync(reader.Email);
            var pile = CanSeeAll ? await TodoStore.ListAllAsync() : await TodoStore.ListMineAsync();
            var openInOrder = TodoSortOrder.Apply(pile, newestFirst).Where(todo => !todo.IsComplete).ToList();
            neighbours = TodoNeighbours.Of(openInOrder, TodoItemId);
        }
        catch { neighbours = TodoNeighbours.None; }
    }

    private void OpenNeighbour(TodoItem neighbour) => Nav.NavigateTo($"/todos/{neighbour.TodoItemId}");

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
