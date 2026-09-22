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
        // Every titled row names at least one assignee (2026-09-22): a row with none would raise
        // an item nobody owns, and Miscellaneous is the choice for that.
        if (command.Items is not null && command.Items.Any(item =>
                !string.IsNullOrWhiteSpace(item.Title) && (item.Assignees is null || item.Assignees.Count == 0)))
            errors.Add(TodoRoles.RoleIsRequiredMessage);
        // A row may name several assignees (it fans out into one item per assignee) — every one of
        // them has to carry a role from the assignable pool. A pinned person always arrives WITH a
        // role by shape (TodoAssignee); that they actually hold it is the handler's directory check.
        if (command.Items is not null && command.Items.Any(item =>
                (item.Assignees ?? Array.Empty<TodoAssignee>()).Any(assignee => !TodoRoles.AssignableAsTodoAssignee.Includes(assignee.Role))))
            errors.Add("To-do items can't be assigned to that role.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
