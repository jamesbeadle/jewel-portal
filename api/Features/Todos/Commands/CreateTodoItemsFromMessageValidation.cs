using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class CreateTodoItemsFromMessageValidation
{
    public ValidationOutcome Check(CreateTodoItemsFromMessage command)
    {
        // ProjectId is deliberately NOT required — blank means general (company-wide) items.
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (command.Items is null || command.Items.Count == 0 || command.Items.All(item => string.IsNullOrWhiteSpace(item.Title)))
            errors.Add("At least one to-do item with a title is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
