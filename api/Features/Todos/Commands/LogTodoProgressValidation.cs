using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class LogTodoProgressValidation
{
    private const int MaxNoteLength = 400;

    public ValidationOutcome Check(LogTodoProgress command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TodoItemId)) errors.Add("TodoItemId is required.");
        if (!TodoProgressKinds.LoggableByHand.Contains(command.Kind))
            errors.Add("Only Started, Chased or Note can be logged by hand — the other kinds are written by the change itself.");
        if (command.Kind == TodoActivityKind.Note && string.IsNullOrWhiteSpace(command.Note))
            errors.Add("A note needs some words.");
        if (command.Note is { Length: > MaxNoteLength })
            errors.Add($"Keep the note under {MaxNoteLength} characters — the detail belongs in the email or the item's notes.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
