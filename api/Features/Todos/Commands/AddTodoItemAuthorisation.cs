using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class AddTodoItemAuthorisation
{
    public bool Allows(SignedInUser user, AddTodoItem command) => TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);
}
