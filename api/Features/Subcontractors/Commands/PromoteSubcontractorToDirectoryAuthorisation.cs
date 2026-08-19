using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class PromoteSubcontractorToDirectoryAuthorisation
{
    // Promoting a prospect IS adding a company to the directory, so the gate mirrors
    // AddSubcontractorToDirectoryAuthorisation exactly.
    private static readonly RoleSet RolesThatMayPromote =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, PromoteSubcontractorToDirectory command) => RolesThatMayPromote.IncludesAny(user.Roles);
}
