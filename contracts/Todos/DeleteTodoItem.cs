using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Todos;

public sealed record DeleteTodoItem(string TodoItemId) : ICommand<Acknowledgement>;
