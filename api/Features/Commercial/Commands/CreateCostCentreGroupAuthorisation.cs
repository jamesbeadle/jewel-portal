using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class CreateCostCentreGroupAuthorisation
{
    // The commercial roles that shape the Financials tab's other inputs, plus the
    // Finance Director (grouping is how finance reconciles subs' packages) and
    // Role.Admin per the newer authorisation convention.
    private static readonly RoleSet RolesThatMayManageGroups =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CreateCostCentreGroup command) =>
        RolesThatMayManageGroups.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RemoveCostCentreGroup command) =>
        RolesThatMayManageGroups.IncludesAny(user.Roles);
}
