using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpdateSubcontractorAuthorisation
{
    // Editing directory records is restricted to Admin (implicit — admins pass every gate),
    // the managing and finance directors, and project managers.
    private static readonly RoleSet RolesThatMayUpdateSubcontractors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, UpdateSubcontractor command) => RolesThatMayUpdateSubcontractors.IncludesAny(user.Roles);
}
