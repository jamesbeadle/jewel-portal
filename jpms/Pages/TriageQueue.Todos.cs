using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // The assignee picker's option pool: the ROLES a to-do can be assigned to
    // (TodoRoles.AssignableAsTodoAssignee, served by ListTodoAssignableRoles) and, under each
    // role, the directory holders it can be pinned to (ListTodoAssignablePeople) — fetched once
    // when the page loads, shaped by TodoAssigneePicker.BuildOptions and shared by every to-do
    // draft row. Assignment is picker-only.
    private IReadOnlyList<SearchSelect.Option> todoAssigneeOptions = Array.Empty<SearchSelect.Option>();
    private IReadOnlyList<TodoAssignablePerson> assignablePeople = Array.Empty<TodoAssignablePerson>();

    // The drafts exactly as they will be posted. Built in one place so the count promised on the
    // summary and the batch the apply actually sends can never disagree.
    private List<TodoItemDraft> CurrentTodoDrafts() => createTodoRows
        .Where(row => !string.IsNullOrWhiteSpace(row.Title))
        .Select(row => new TodoItemDraft(
            row.Title.Trim(),
            NullIfBlank(row.Notes),
            ParseTodoAssignees(row.Assignees),
            ParseDate(row.Due)))
        .ToList();

    private async Task LoadTodoAssignableRolesAsync()
    {
        // A failed load leaves the picker with no options rather than blocking triage — to-dos can
        // still be created, they just go in unassigned.
        try
        {
            var rolesTask = Todos.ListAssignableRolesAsync();
            var peopleTask = Todos.ListAssignablePeopleAsync();
            assignablePeople = await peopleTask;
            todoAssigneeOptions = TodoAssigneePicker.BuildOptions(await rolesTask, assignablePeople);
            await StageFilter.EnsureLoadedAsync();
            StageFilter.OnChange += StageFilterChanged;
        }
        catch { }
    }
}
