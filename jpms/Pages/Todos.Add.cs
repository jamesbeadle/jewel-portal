using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

public partial class Todos
{
    private void OnNewNotesInput(ChangeEventArgs e) => newNotes = e.Value?.ToString() ?? "";

    private void OpenAdd()
    {
        newTitle = newNotes = newAssignee = newDue = "";
        // The scope filter is the reader's stated context: raising an item while one project is
        // picked means an item on that project. All / Company-wide leave the picker to say.
        newProject = scopeFilter is ScopeAll or ScopeGeneral ? "" : scopeFilter;
        addError = null;
        addedNote = addedNoteHref = null;
        addOpen = true;
    }

    private void CloseAdd()
    {
        addOpen = false;
        addError = null;
    }

    private async Task Add()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(newTitle)) { addError = "A title is required."; return; }
        // Blank = company-wide, which is the managing director's / administrators' call. Said here
        // rather than by hiding the blank row, so the answer is "not yours to make", not "missing".
        if (newProject == "" && !CanSeeAll)
        {
            addError = "Pick the project this item belongs to — only the managing director adds company-wide items.";
            return;
        }
        addError = null;
        addedNote = addedNoteHref = null;
        // Held for the "added, but not on your list" note below — Run reloads the list around it.
        TodoItem? added = null;
        await Run(async () =>
        {
            added = await PostNewItemAsync();
            CloseAdd();
        });
        // A failed add leaves the modal open with the page-level error mirrored inside it. A
        // failure AFTER the add — the reload — has already closed the modal; the red bar carries
        // that one, and the stale list it leaves behind is no basis for the note below.
        if (error is not null)
        {
            if (addOpen) addError = error;
            return;
        }
        if (added is TodoItem raised && !IsShowing(raised)) NoteWhereItWent(raised);
    }

    private Task<TodoItem> PostNewItemAsync()
    {
        var assignee = TodoAssigneePicker.Parse(newAssignee);
        var title = newTitle.Trim();
        var notes = NullIfBlank(newNotes);
        var due = ParseDate(newDue);
        return newProject == ""
            ? TodoStore.AddGeneralAsync(new AddGeneralTodoItem(title, notes, assignee?.Role, assignee?.PersonEmail, due))
            : TodoStore.AddAsync(new AddTodoItem(newProject, title, notes, assignee?.Role, assignee?.PersonEmail, due));
    }

    private bool IsShowing(TodoItem item) =>
        (boardView ? FilteredItems : VisibleItems).Any(shown => shown.TodoItemId == item.TodoItemId);

    // On a CanSeeAll list every added item comes back, so this never fires there. On a reader's
    // own list an item assigned to someone else's role does not — say where it went instead.
    // Tested against what the reader can SEE, not what came back: an item raised for Ravenswood
    // while the filter says Company-wide (or while Done is the open tab) is on the list and
    // still invisible, which looks exactly like the add having done nothing.
    private void NoteWhereItWent(TodoItem raised)
    {
        addedNote = items.Any(item => item.TodoItemId == raised.TodoItemId)
            ? $"{raised.Reference} added to {ScopeLabel(raised)}. These filters are hiding it."
            : $"{raised.Reference} added to {ScopeLabel(raised)}. It's assigned to another role, so it isn't on your own list.";
        addedNoteHref = string.IsNullOrWhiteSpace(raised.ProjectId) ? null : $"/projects/{raised.ProjectId}/todos";
    }
}
