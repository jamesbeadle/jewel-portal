using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class ConsolidateDirectoryRecordsAuthorisation
{
    // Consolidation deletes directory records and rewrites what points at them, so it is held to
    // the directory-management gate (Admin passes implicitly — admins pass every gate). PMs may
    // edit records but deliberately can't merge them.
    private static readonly RoleSet RolesThatMayConsolidate =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, ConsolidateDirectoryRecords command) => RolesThatMayConsolidate.IncludesAny(user.Roles);
}
