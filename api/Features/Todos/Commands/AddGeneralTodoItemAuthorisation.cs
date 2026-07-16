using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// General (no-project) items are added from the To-dos browser page, which is the managing
// director's / administrators' surface — so the add gate matches the browse-all gate rather than
// the broader project to-do management gate.
public sealed class AddGeneralTodoItemAuthorisation
{
    public bool Allows(SignedInUser user, AddGeneralTodoItem command) => TodoRoles.AllowedToSeeAllTodos.IncludesAny(user.Roles);
}
