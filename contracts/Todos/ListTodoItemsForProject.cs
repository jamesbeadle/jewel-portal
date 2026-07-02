using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

public sealed record ListTodoItemsForProject(string ProjectId) : IQuery<IReadOnlyList<TodoItem>>;
