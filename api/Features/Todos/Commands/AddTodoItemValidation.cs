using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class AddTodoItemValidation
{
    public ValidationOutcome Check(AddTodoItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (command.AssigneeRole is Role role && !TodoRoles.AssignableAsTodoAssignee.Includes(role))
            errors.Add("To-do items can't be assigned to that role.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
