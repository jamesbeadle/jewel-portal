using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Project to-do items: listed and managed on the project's To-do tab. Items created from an email
// at the triage stage arrive through IIntakeQueue.CreateTodoItemsFromMessageAsync and show up here.
public interface ITodoStore
{
    Task<IReadOnlyList<TodoItem>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    /// <summary>The internal staff a to-do can be assigned to — feeds the assignee pickers.</summary>
    Task<IReadOnlyList<DirectoryUser>> ListAssigneesAsync(CancellationToken cancellationToken = default);
    Task<TodoItem> AddAsync(AddTodoItem command, CancellationToken cancellationToken = default);
    Task<TodoItem> UpdateAsync(UpdateTodoItem command, CancellationToken cancellationToken = default);
    Task<Acknowledgement> DeleteAsync(string todoItemId, CancellationToken cancellationToken = default);
}
