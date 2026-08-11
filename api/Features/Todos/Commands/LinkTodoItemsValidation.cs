using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class LinkTodoItemsValidation
{
    public ValidationOutcome Check(LinkTodoItems command)
    {
        // LinkedByEmail is deliberately NOT checked: the endpoint stamps it from the signed-in
        // user, never trusting the client body. That both items exist is the handler's database
        // check.
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TodoItemId)) errors.Add("TodoItemId is required.");
        if (string.IsNullOrWhiteSpace(command.LinkedTodoItemId)) errors.Add("LinkedTodoItemId is required.");
        if (errors.Count == 0 && command.TodoItemId == command.LinkedTodoItemId)
            errors.Add("A to-do item cannot be linked to itself.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
