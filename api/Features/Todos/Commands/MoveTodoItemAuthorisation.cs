using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Moving an item between projects is a manage-gate action (decision 2026-08-07): the FD, a PM or
// a site manager re-filing an item raised on the wrong project is the same act as handing it to a
// different role, which they may already do. Moving an item to COMPANY-WIDE (blank ProjectId) is
// narrower — a general item lives on the To-dos browser, the managing director's / administrators'
// surface — so that destination matches the AddGeneralTodoItem gate instead.
public sealed class MoveTodoItemAuthorisation
{
    public bool Allows(SignedInUser user, MoveTodoItem command) =>
        string.IsNullOrWhiteSpace(command.ProjectId)
            ? TodoRoles.AllowedToSeeAllTodos.IncludesAny(user.Roles)
            : TodoRoles.AllowedToManageTodos.IncludesAny(user.Roles);
}
