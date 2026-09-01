using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Logging progress is the assignee's act above all, so the gate is the same pair UpdateTodoItem
// uses for ticking an item off: the manage roles, or anyone the item is CURRENTLY assigned to —
// checked against the stored row, never the posted body.
public sealed class LogTodoProgressAuthorisation
{
    private readonly JpmsContext context;
    public LogTodoProgressAuthorisation(JpmsContext context) { this.context = context; }

    public bool Allows(SignedInUser user, LogTodoProgress command) =>
        TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);

    public async Task<bool> AllowsAsAssigneeAsync(SignedInUser user, LogTodoProgress command, CancellationToken cancellationToken)
    {
        var currentAssigneeRole = await context.TodoItems.AsNoTracking()
            .Where(item => item.TodoItemId == command.TodoItemId)
            .Select(item => item.AssigneeRole)
            .FirstOrDefaultAsync(cancellationToken);
        return currentAssigneeRole is int role && user.Roles.Contains((Role)role);
    }
}
