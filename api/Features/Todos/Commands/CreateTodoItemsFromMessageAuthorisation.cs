using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Creating to-dos from an email happens at the triage stage, so it carries the triage gate
// (administrators and project managers) rather than the broader to-do management gate.
public sealed class CreateTodoItemsFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayCreateFromMessage =
        RoleSet.Of(Role.Admin, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, CreateTodoItemsFromMessage command) => RolesThatMayCreateFromMessage.IncludesAny(user.Roles);
}
