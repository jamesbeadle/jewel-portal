using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// To-do items: listed and managed on the project's To-do tab, the /todos browser page, and the
// "My to-dos" dashboard panel. Items created from an email at the triage stage arrive through
// IIntakeQueue.CreateTodoItemsFromMessageAsync and show up here. General (company-wide) items
// carry a blank ProjectId. Items are assigned to a ROLE, not a person — whoever holds the role
// sees them, so assignments survive staff changes.
public interface ITodoStore
{
    Task<IReadOnlyList<TodoItem>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    /// <summary>Every item assigned to any role the signed-in user holds — general and project
    /// items alike. Backs the "My to-dos" dashboard panel and the browser for non-admin roles.</summary>
    Task<IReadOnlyList<TodoItem>> ListMineAsync(CancellationToken cancellationToken = default);
    /// <summary>Every item in the system — the MD's / administrators' browser read (403 for
    /// anyone else; see TodoRoles.AllowedToSeeAllTodos in the api).</summary>
    Task<IReadOnlyList<TodoItem>> ListAllAsync(CancellationToken cancellationToken = default);
    /// <summary>The roles a to-do can be assigned to — feeds the assignee role pickers.</summary>
    Task<IReadOnlyList<Role>> ListAssignableRolesAsync(CancellationToken cancellationToken = default);
    Task<TodoItem> AddAsync(AddTodoItem command, CancellationToken cancellationToken = default);
    /// <summary>Add a general (company-wide, no-project) item from the /todos browser page.</summary>
    Task<TodoItem> AddGeneralAsync(AddGeneralTodoItem command, CancellationToken cancellationToken = default);
    Task<TodoItem> UpdateAsync(UpdateTodoItem command, CancellationToken cancellationToken = default);
    Task<Acknowledgement> DeleteAsync(string todoItemId, CancellationToken cancellationToken = default);
}
