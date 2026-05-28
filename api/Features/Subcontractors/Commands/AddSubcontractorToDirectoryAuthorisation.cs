using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryAuthorisation
{
    private static readonly RoleSet RolesThatMayAddSubcontractors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, AddSubcontractorToDirectory command) => RolesThatMayAddSubcontractors.IncludesAny(user.Roles);
}
