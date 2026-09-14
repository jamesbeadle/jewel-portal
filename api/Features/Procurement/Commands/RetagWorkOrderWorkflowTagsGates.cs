using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>The sweep rewrites tags across the shared mailbox, so it is the triage roles' to run —
/// the same people who own the mailbox views (the request sweep's gate).</summary>
public sealed class RetagWorkOrderWorkflowTagsAuthorisation
{
    public static readonly RoleSet Sweepers = TriageRoles.AllowedToTriage;

    public bool Allows(SignedInUser user, RetagWorkOrderWorkflowTags command) =>
        user.Roles.Contains(Role.Admin) || Sweepers.IncludesAny(user.Roles);
}
