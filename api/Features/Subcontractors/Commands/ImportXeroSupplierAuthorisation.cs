using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class ImportXeroSupplierAuthorisation
{
    // Importing creates directory records, so this mirrors AddSubcontractorToDirectory's gate
    // (Admin passes implicitly — admins pass every gate).
    private static readonly RoleSet RolesThatMayImportFromXero =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, ImportXeroSupplier command) => RolesThatMayImportFromXero.IncludesAny(user.Roles);
}
