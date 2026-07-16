using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UpdateTodoItemAuthorisation
{
    private readonly JpmsContext context;
    public UpdateTodoItemAuthorisation(JpmsContext context) { this.context = context; }

    public bool Allows(SignedInUser user, UpdateTodoItem command) => TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);

    // A user outside the manage gate may still update an item that is CURRENTLY assigned to them —
    // that's what lets an assignee tick their own item off from the dashboard / To-dos browser.
    // Checked against the stored row, not the command body, so it can't be used to grab someone
    // else's item by posting a different assignee.
    public async Task<bool> AllowsAsAssigneeAsync(SignedInUser user, UpdateTodoItem command, CancellationToken cancellationToken)
    {
        var currentAssignee = await context.TodoItems.AsNoTracking()
            .Where(t => t.TodoItemId == command.TodoItemId)
            .Select(t => t.AssigneeEmail)
            .FirstOrDefaultAsync(cancellationToken);
        return currentAssignee is not null
            && string.Equals(currentAssignee, user.Email, StringComparison.OrdinalIgnoreCase);
    }
}
