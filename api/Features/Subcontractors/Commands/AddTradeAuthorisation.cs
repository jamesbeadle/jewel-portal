using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddTradeAuthorisation
{
    // Same people who curate the directory curate the trade list — the add-company modal
    // creates trades inline, so this gate mirrors AddSubcontractorToDirectoryAuthorisation.
    private static readonly RoleSet RolesThatMayAddTrades =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, AddTrade command) => RolesThatMayAddTrades.IncludesAny(user.Roles);
}
