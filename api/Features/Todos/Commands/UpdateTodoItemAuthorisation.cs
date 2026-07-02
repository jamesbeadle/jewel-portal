using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UpdateTodoItemAuthorisation
{
    public bool Allows(SignedInUser user, UpdateTodoItem command) => TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);
}
