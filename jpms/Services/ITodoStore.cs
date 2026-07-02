using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Project to-do items: listed and managed on the project's Overview tab. Items created from an email
// at the triage stage arrive through IIntakeQueue.CreateTodoItemsFromMessageAsync and show up here.
public interface ITodoStore
{
    Task<IReadOnlyList<TodoItem>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<TodoItem> AddAsync(AddTodoItem command, CancellationToken cancellationToken = default);
    Task<TodoItem> UpdateAsync(UpdateTodoItem command, CancellationToken cancellationToken = default);
    Task<Acknowledgement> DeleteAsync(string todoItemId, CancellationToken cancellationToken = default);
}
