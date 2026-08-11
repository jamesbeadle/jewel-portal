using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UnlinkTodoItemsValidation
{
    public ValidationOutcome Check(UnlinkTodoItems command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TodoItemId)) errors.Add("TodoItemId is required.");
        if (string.IsNullOrWhiteSpace(command.LinkedTodoItemId)) errors.Add("LinkedTodoItemId is required.");
        if (errors.Count == 0 && command.TodoItemId == command.LinkedTodoItemId)
            errors.Add("A to-do item cannot be unlinked from itself.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
