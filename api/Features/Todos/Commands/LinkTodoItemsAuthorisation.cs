using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Linking two items is a manage-gate action, like reassigning or moving one: it shapes how the
// work reads, but reveals nothing the manage roles can't already list.
public sealed class LinkTodoItemsAuthorisation
{
    public bool Allows(SignedInUser user, LinkTodoItems command) =>
        TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);
}
