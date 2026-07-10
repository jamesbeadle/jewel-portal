using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class LinkXeroLineToWorkOrderAuthorisation
{
    // The Financials tab's commercial roles, plus the Finance Director: tying Xero
    // purchase invoices to the work orders they pay against is a finance activity
    // (the same audience as the Xero ledger view and valuation invoices).
    private static readonly RoleSet RolesThatMayLink =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, LinkXeroLineToWorkOrder command) =>
        RolesThatMayLink.IncludesAny(user.Roles);
}
