using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>One authorisation surface for the package commands — building, dissolving
/// and locking packages shapes what everyone reads from the Financials tab, the same
/// audience as the tab's other inputs plus the Finance Director. Administrators pass
/// via Role.Admin, per the newer authorisation convention.</summary>
public sealed class ReconciliationPackageAuthorisation
{
    private static readonly RoleSet RolesThatMayManagePackages =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, SaveReconciliationPackage command) =>
        RolesThatMayManagePackages.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RemoveReconciliationPackage command) =>
        RolesThatMayManagePackages.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, SetReconciliationPackageLock command) =>
        RolesThatMayManagePackages.IncludesAny(user.Roles);
}
