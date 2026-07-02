using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UpdateTodoItemValidation
{
    public ValidationOutcome Check(UpdateTodoItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TodoItemId)) errors.Add("TodoItemId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
