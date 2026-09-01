using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class MoveTodoItemValidation
{
    public ValidationOutcome Check(MoveTodoItem command)
    {
        // ProjectId is deliberately NOT required: blank means company-wide (general, no project),
        // exactly as it does on the TodoItem row itself. That the project exists, when one is
        // named, is the handler's database check.
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TodoItemId)) errors.Add("TodoItemId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
