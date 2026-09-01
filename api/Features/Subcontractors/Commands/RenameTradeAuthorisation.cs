using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RenameTradeAuthorisation
{
    // The same people who curate the trade list may tidy it — mirrors AddTradeAuthorisation
    // (administrators pass every role gate via SignedInUserResolver).
    private static readonly RoleSet RolesThatMayRenameTrades =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, RenameTrade command) => RolesThatMayRenameTrades.IncludesAny(user.Roles);
}
