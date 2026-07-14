using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class RecodeWorkOrderLineAuthorisation
{
    // The commercial roles that shape the Financials tab, plus the Finance Director —
    // re-coding lines is how finance puts a sub's single-code order onto the centres
    // the work actually belongs to — and Role.Admin per the newer convention.
    private static readonly RoleSet RolesThatMayRecodeLines =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecodeWorkOrderLine command) =>
        RolesThatMayRecodeLines.IncludesAny(user.Roles);
}
