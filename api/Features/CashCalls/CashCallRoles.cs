using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CashCalls;

// Cash calls are a commercial/finance activity: Administrator, Managing Director, Finance Director
// and Project Manager. Administrators carry every role server-side.
internal static class CashCallRoles
{
    public static readonly RoleSet AllowedToManageCashCalls =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);
}
