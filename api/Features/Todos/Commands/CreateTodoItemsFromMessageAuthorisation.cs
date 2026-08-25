using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Creating to-dos from an email happens at the triage stage, so it carries the triage gate rather
// than the broader to-do management gate. Widened 2026-08-25 to the full Control Centre gate
// (TriageRoles.AllowedToTriage — the directors and the finance director included): the Finance
// Director could open the Control Centre and stage a to-do there, then have Apply refused.
public sealed class CreateTodoItemsFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayCreateFromMessage = TriageRoles.AllowedToTriage;

    public bool Allows(SignedInUser user, CreateTodoItemsFromMessage command) => RolesThatMayCreateFromMessage.IncludesAny(user.Roles);
}
