using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Move (re-file) a to-do item to a different project — or to company-wide (ProjectId "" = general,
// no project). Everything else about the item stays put: its TODO-#### reference is global (not
// per-project), so the mailbox tag — and with it the linked emails — follows the item wherever it
// goes, as do its assignee, due date and open/done state.
//
// Anyone in the manage gate (TodoRoles.AllowedToManageTodos) may move an item between projects;
// moving one to company-wide is the managing director's / administrators' call only, matching the
// AddGeneralTodoItem gate — see MoveTodoItemAuthorisation in the api.
public sealed record MoveTodoItem(
    string TodoItemId,
    string ProjectId) : ICommand<TodoItem>;
