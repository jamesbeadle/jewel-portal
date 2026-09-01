using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class DeleteTradeAuthorisation
{
    // The same people who curate the trade list may tidy it — mirrors AddTradeAuthorisation
    // (administrators pass every role gate via SignedInUserResolver).
    private static readonly RoleSet RolesThatMayDeleteTrades =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, DeleteTrade command) => RolesThatMayDeleteTrades.IncludesAny(user.Roles);
}
