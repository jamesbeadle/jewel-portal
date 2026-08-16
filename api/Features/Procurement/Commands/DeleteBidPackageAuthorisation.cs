using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class DeleteBidPackageAuthorisation
{
    // The same set that may create a package may delete one — deletion exists for packages
    // raised in error (including unwanted AI suggestions), which is the creator tidying up.
    // The handler, not the role, is what protects committed money: Awarded packages and
    // anything a work order references are refused regardless of who asks.
    private static readonly RoleSet RolesThatMayDeletePackages = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, DeleteBidPackage command) => RolesThatMayDeletePackages.IncludesAny(user.Roles);
}
