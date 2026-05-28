using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class MarkLeadAsLostAuthorisation
{
    private static readonly RoleSet RolesThatMayMarkLeadsAsLost =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, MarkLeadAsLost command) =>
        RolesThatMayMarkLeadsAsLost.IncludesAny(user.Roles);
}
