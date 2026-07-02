using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class DeleteTodoItemAuthorisation
{
    public bool Allows(SignedInUser user, DeleteTodoItem command) => TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);
}
