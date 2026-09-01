using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Unlinking is the same manage-gate action as linking: it reshapes how the work reads and
// nothing more.
public sealed class UnlinkTodoItemsAuthorisation
{
    public bool Allows(SignedInUser user, UnlinkTodoItems command) =>
        TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);
}
