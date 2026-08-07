using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryAuthorisation
{
    // The finance director was added (2026-07-28) to match the update gate — the FD could already
    // edit directory records, and the RBAC review's target DirectoryRoles includes the FD.
    private static readonly RoleSet RolesThatMayAddSubcontractors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, AddSubcontractorToDirectory command) => RolesThatMayAddSubcontractors.IncludesAny(user.Roles);
}
