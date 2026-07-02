using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpTodoStore : ITodoStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpTodoStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<TodoItem>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoItemsForProject(projectId), cancellationToken);

    public Task<TodoItem> AddAsync(AddTodoItem command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<TodoItem> UpdateAsync(UpdateTodoItem command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<Acknowledgement> DeleteAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DeleteTodoItem(todoItemId), cancellationToken);
}
